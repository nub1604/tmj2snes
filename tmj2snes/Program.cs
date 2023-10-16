
using System.IO;
using tmj2snes.JsonFiles;

const int N_METATILES = 1024; // maximum tiles
const int N_OBJECTS = 64;     // maximum objects
const int N_REGIONS = 16;
uint TILED_FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
uint TILED_FLIPPED_VERTICALLY_FLAG = 0x40000000;


string[] arguments=args;
bool disableTileset = false;
eConvertMode mode = eConvertMode.None;
string file = "";

SetupUpConverter();
ConvertMode();





void SetupUpConverter()
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
                CheckSetFile(i + 1);
                break;
            case "-w":  //world
                mode = eConvertMode.World;
                CheckSetFile(i + 1);
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
            var tileset = FileHandler.LoadFile<Tileset>(file);
            // Todo: Convert Tileset Only



            break;
        case eConvertMode.World:
            var world = FileHandler.LoadFile<World>(file);
            if (world == null) return;
            var path = new FileInfo(file).Directory.FullName;

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

void ConvertTileset(string file)
{

  
    var ts = FileHandler.LoadFile<Tileset>(file);


   // ts.Tiles[0].Properties[]. = new List<Tile>();




}


void ConvertMap(string file)
{
    var map = FileHandler.LoadFile<TileMap>(file);
    if (map == null) return;
    var path = new FileInfo(file).Directory.FullName;
    var mName = Path.GetFileNameWithoutExtension(file);
    if (!disableTileset)
    {
        var ts = map.Tilesets[0];
        if (!string.IsNullOrEmpty(ts.Source))
        
        {
            var tsPath = Path.Combine(path, ts.Source);
            ConvertTileset(tsPath);
        }
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
                PutWord((ushort)(layer.Data.Count*2), mapStream);
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
            using (FileStream objStream = new(Path.Combine(path, $"{mName}.r16"), FileMode.Create))
            {
                pvsneslib_region_t pvreg;
                foreach (var obj in layer.Objects)
                {


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




byte HI_BYTE(ushort n) =>  (byte)((n >> 8) & 0x00ff); // extracts the hi-byte of a word
byte LOW_BYTE(ushort n) => (byte)(n & 0x00ff);      // extracts the low-byte of a word
void PutWord(ushort data, Stream fp)
{
    fp.WriteByte(LOW_BYTE(data));
    fp.WriteByte(HI_BYTE(data));
} // end of PutWord
enum eConvertMode
{
    Map, World, Tileset, None

};
struct pvsneslib_object_t
{
    public ushort x;    // x coordinate in pixels.
    public ushort y;    // y coordinate in pixels.
    public ushort type; // type of object (0=main character, 1..63 other types)
    public ushort minx; // horizontal or vertical min x coordinate in pixels.
    public ushort maxx; // horizontal or vertical max x coordinate in pixels.
};

struct pvsneslib_region_t
{
   public ushort cls;    // x coordinate in pixels.
   public ushort x;    // y coordinate in pixels.
   public ushort y; // type of object (0=main character, 1..63 other types)
   public ushort width; // horizontal or vertical min x coordinate in pixels.
   public ushort height; // horizontal or vertical max x coordinate in pixels.
};