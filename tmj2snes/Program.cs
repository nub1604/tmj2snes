using System.Globalization;
using tmj2snes.JsonFiles;

const int N_METATILES = 1024; // maximum tiles

const int N_OBJECTS = 64;     // maximum objects
const int N_REGIONS = 16;
uint TILED_FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
uint TILED_FLIPPED_VERTICALLY_FLAG = 0x40000000;

string[] arguments = args;
bool disableTileset = false;
eConvertMode mode = eConvertMode.None;
string file = "";

pvsneslib_tile_t[] tilesetbuffer = new pvsneslib_tile_t[N_METATILES];
SetupMode();
ConvertMode();

void SetupMode()
{
    mode = eConvertMode.Map;
    for (int i = 0; i < arguments.Length; i++)
    {
        switch (arguments[i])
        {
            case "-t!": //map no Tileset
                disableTileset = true;
                break;

            case "-t":  //only Tileset
                mode = eConvertMode.Tileset;
                break;

            case "-w":  //world
                mode = eConvertMode.World;
                break;

            default:    //map
                CheckSetFile(i);
                break;
        }
    }
}
void ConvertMode()
{
    switch (mode)
    {
        case eConvertMode.Tileset:
            var ts = ExtractTilesetPropsFromFile(file);
            ConvertTileset_T16(file, ts);
            ConvertTileset_B16(file, ts);

            // Todo: Convert Tileset Only

            break;

        case eConvertMode.World:
            var world = FileHandler.LoadFile<World>(file);
            if (world == null) return;
            var path = new FileInfo(file).Directory!.FullName;

            foreach (var item in world.Maps)
            {
                ConvertMap(Path.Combine(path, item.FileName));
            }
            break;

        case eConvertMode.Map:
            ConvertMap(Path.Combine(file));
            break;
    }
}

void ConvertTileset_T16(string file, Tileset? tileset)
{
    if (tileset == null) return;
    if (!File.Exists(file)) return;
    var mName = Path.GetFileNameWithoutExtension(file);
    var path = new FileInfo(file).Directory!.FullName;
    using (FileStream t16Stream = new(Path.Combine(path, $"{mName}.t16"), FileMode.Create))
    {
        for (int i = 0; i < tileset.Tilecount; i++)
        {
            var res = tileset.Tiles[i].Id & 0x03FF;
            res |= tilesetbuffer[i].priority > 0 ? 0x2000 : 0x0000;
            res |= Math.Clamp(tilesetbuffer[i].palette, (ushort)0, (ushort)7) << 10;
            PutWord((ushort)res, t16Stream);
        }
    }
}
void ConvertTileset_B16(string file, Tileset? tileset)
{
    if (tileset == null) return;
    if (!File.Exists(file)) return;
    var mName = Path.GetFileNameWithoutExtension(file);

    var path = new FileInfo(file).Directory!.FullName;
    using (FileStream b16Stream = new(Path.Combine(path, $"{mName}.b16"), FileMode.Create))
    {
        for (int i = 0; i < tileset.Tilecount; i++)
        {
            PutWord(tilesetbuffer[i].attribute, b16Stream);
        }
    }
}
Tileset? ExtractTilesetPropsFromFile(string file)
{
    Tileset? ts = FileHandler.LoadFile<Tileset>(file);
    return ExtractTilesetProps(ts);
}
Tileset? ExtractTilesetProps(Tileset? ts)
{
    if (ts == null) return null;
    if (ts.Tilecount > N_METATILES)
    {
        throw new ArgumentOutOfRangeException($"tmx2snes: error 'too much tiles in tileset ({ts.Tilecount} tiles, {N_METATILES} max expected)'");
    }

    // browse and store in table
    for (int i = 0; i < ts.Tilecount; i++)
    {
        var tile = ts.Tiles[i];
        foreach (var prop in tile.Properties)
        {
            if (prop.Name == "attribute")
            {
                tilesetbuffer[i].attribute = UInt16.Parse(prop.Value, NumberStyles.HexNumber);
            }
            if (prop.Name == "priority")
            {
                tilesetbuffer[i].priority = Convert.ToUInt16(prop.Value);
            }
            if (prop.Name == "palette")
            {
                tilesetbuffer[i].palette = Convert.ToUInt16(prop.Value);
            }
        }
    }
    return ts;
}

void ConvertMap(string file)
{
    var map = FileHandler.LoadFile<TileMap>(file);
    if (map == null) return;

    if ((map.Width * map.Height) > 16384)
    {
        throw new ArgumentOutOfRangeException($"tmj2snes: error 'map is too big (max 32K)! ({((map.Width * map.Height * 2) / 1024)}K)'\n");
    }
    if (map.Height > 256)
    {
        throw new ArgumentOutOfRangeException($"tmj2snes: error 'map height is too big! (max 256) ({map.Height})'\n");
    }
    if ((map.Tilewidth != 8) || (map.Tileheight != 8))
    {
        throw new FormatException($"tmj2snes: error 'tile width or height are not 8px! ({map.Tilewidth} {map.Tileheight})\n");
    }

    var path = new FileInfo(file).Directory!.FullName;
    var mName = Path.GetFileNameWithoutExtension(file);
    if (!disableTileset)
    {
        var ts = map.Tilesets[0];
        if (!string.IsNullOrEmpty(ts.Source))
        {
            var tsPath = Path.Combine(path, ts.Source);
            ts = ExtractTilesetPropsFromFile(tsPath);
        }
        ConvertTileset_T16(file, ts);
        ConvertTileset_B16(file, ts);
    }

    foreach (var layer in map.Layers)
    {
        if (layer.Type == "tilelayer")
        {
            //todo: .m16
            using (FileStream mapStream = new(Path.Combine(path, $"{layer.Name}.m16"), FileMode.Create))
            {
                PutWord((ushort)(map.Width * map.Tilewidth), mapStream);
                PutWord((ushort)(map.Height * map.Tileheight), mapStream);
                PutWord((ushort)(layer.Data.Count * 2), mapStream);
                for (int i = 0; i < layer.Data.Count; i++)
                {
                    var tileattr = layer.Data[i];
                    if (tileattr != null)
                    {
                        var td = tileattr.ToString();

                        var finalf = (Convert.ToUInt32(td) - 1) & 0x03FF;
                        ushort tilesnes = Convert.ToUInt16(finalf); // keep on the low 16bits of tile number

                        if ((Convert.ToUInt32(td) & TILED_FLIPPED_HORIZONTALLY_FLAG) != 0) // Flipx attribute
                            tilesnes |= (1 << 14);
                        if ((Convert.ToUInt32(td) & TILED_FLIPPED_VERTICALLY_FLAG) != 0) // Flipy attribute
                            tilesnes |= (1 << 15);

                        PutWord(tilesnes, mapStream);
                    }
                    // no (certainly an error in the map with no tile assignment), write 0
                    else
                        PutWord(0x0000, mapStream);
                }
            }
        }
        else if (layer.Name == "Regions" && layer.Type == "objectgroup")
        {
            if (layer.Objects.Count > N_REGIONS)
            {
                throw new ArgumentOutOfRangeException($"tmj2snes: error to many regions (max {N_REGIONS}) ({layer.Objects.Count})");
            }
            using (FileStream objStream = new(Path.Combine(path, $"{mName}.r16"), FileMode.Create))
            {
                pvsneslib_region_t pvreg;
                for (int i = 0; i < layer.Objects.Count; i++)
                {
                    TiledObject? obj = layer.Objects[i];
                    pvreg.cls = Convert.ToUInt16(obj.Type);
                    pvreg.x = Convert.ToUInt16(obj.X);
                    pvreg.y = Convert.ToUInt16(obj.Y);
                    pvreg.width = Convert.ToUInt16(obj.Width);
                    pvreg.height = Convert.ToUInt16(obj.Height);

                    PutWord(pvreg.cls, objStream);
                    PutWord(pvreg.x, objStream);
                    PutWord(pvreg.y, objStream);
                    PutWord(pvreg.width, objStream);
                    PutWord(pvreg.height, objStream);
                }
                //  PutWord(0xFFFF, objStream); //termination
                PutWord(ushort.MaxValue, objStream);
            }
        }
        else if (layer.Name == "Entities" && layer.Type == "objectgroup")
        {
            if (layer.Objects.Count > N_OBJECTS)
            {
                throw new ArgumentOutOfRangeException($"tmj2snes: error to many objects (max {N_OBJECTS}) ({layer.Objects.Count})");
            }
            using (FileStream objStream = new(Path.Combine(path, $"{mName}.o16"), FileMode.Create))
            {
                foreach (var obj in layer.Objects)
                {
                    pvsneslib_object_t pvobj = new pvsneslib_object_t();

                    pvobj.x = Convert.ToUInt16(obj.X);
                    pvobj.y = Convert.ToUInt16(obj.Y);
                    pvobj.type = Convert.ToUInt16(obj.Type);
                    pvobj.minx = 0;
                    pvobj.maxx = 0;

                    foreach (var prop in obj.Properties)
                    {
                        if (prop.Name == "minx")
                            pvobj.minx = Convert.ToUInt16(prop.Value);
                        if (prop.Name == "maxx")
                            pvobj.maxx = Convert.ToUInt16(prop.Value);
                    }
                    PutWord(pvobj.x, objStream);
                    PutWord(pvobj.y, objStream);
                    PutWord(pvobj.type, objStream);
                    PutWord(pvobj.minx, objStream);
                    PutWord(pvobj.maxx, objStream);
                }
                //  PutWord(0xFFFF, objStream);
                PutWord(ushort.MaxValue, objStream);
            }
        }
    }
}

void CheckSetFile(int index)
{
    if (index >= arguments.Length)
    {
        throw new ArgumentOutOfRangeException($"filepath to {mode.ToString()} expected");
    }
    file = arguments[index];
}

byte HI_BYTE(ushort n) => (byte)((n >> 8) & 0x00ff); // extracts the hi-byte of a word
byte LOW_BYTE(ushort n) => (byte)(n & 0x00ff);      // extracts the low-byte of a word
void PutWord(ushort data, Stream fp)
{
    fp.WriteByte(LOW_BYTE(data));
    fp.WriteByte(HI_BYTE(data));
} // end of PutWord

internal enum eConvertMode
{
    Map, World, Tileset, None
};

internal struct pvsneslib_object_t
{
    public ushort x;
    public ushort y;
    public ushort type;
    public ushort minx;
    public ushort maxx;
};

internal struct pvsneslib_region_t
{
    public ushort cls;
    public ushort x;
    public ushort y;
    public ushort width;
    public ushort height;
};

internal struct pvsneslib_tile_t
{
    public ushort attribute;    // x coordinate in pixels.
    public ushort palette;    // y coordinate in pixels.
    public ushort priority;
};