﻿using NLua;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using tmj2snes.CustomConverter;
using tmj2snes.JsonFiles;



LuaTmj extensions = new();
extensions.LoadAll();
extensions.ExecuteAllBegin();


const int N_METATILES = 1024; // maximum tiles
const int N_OBJECTS = 64;     // maximum objects
const int N_REGIONS = 16;     // maximum regions
const uint TILED_FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
const uint TILED_FLIPPED_VERTICALLY_FLAG = 0x40000000;

List<string> _dataSizeExports = [];
string[] _arguments = args;
bool _disableTileset = false;
string _file = "";
string _asmdir = "";
long _currentBankCounter = 0;
long _totalBankCounter = 0;
long _SectionCounter = 0;

List<(eConvertMode, string)> WorkItems = [];
pvsneslib_tile_t[] tilesetbuffer = new pvsneslib_tile_t[N_METATILES];
StringBuilder _dataWriter = new ();
StringBuilder _importWriter = new ();

_importWriter.AppendLine("#ifndef TILEDEXPORT_H");
_importWriter.AppendLine("#define TILEDEXPORT_H");
_importWriter.AppendLine("#include <snes.h>");

SetupWorkItems();
ConvertWorkItems();

_dataWriter.AppendLine($".ends ; .mapsection{_SectionCounter}, sectionsize {_currentBankCounter}/{ushort.MaxValue >> 1}");
_dataWriter.AppendLine();
_dataWriter.AppendLine($"; {_totalBankCounter}bytes total");
_importWriter.AppendLine("// reference size defines");
for (int i = 0; i < _dataSizeExports.Count; i++)
{
    if (i % 5 == 0)
    {
        _dataWriter.AppendLine();
        _dataWriter.Append(".EXPORT ");
    }
    _dataWriter.Append($"{_dataSizeExports[i]}_size ");
    _importWriter.AppendLine($"extern char {_dataSizeExports[i]}_size;");
}
 _importWriter.AppendLine("#endif // TILEDEXPORT_H");

var path = new FileInfo(_file).Directory!.FullName;



using (FileStream fs = new(Path.Combine(path, "data.asm"), FileMode.Create))
{
    byte[] info = new UTF8Encoding(true).GetBytes(_dataWriter.ToString());
    fs.Write(info, 0, info.Length);
}
using (FileStream fs = new(Path.Combine(path, "exports.h"), FileMode.Create))
{
    byte[] info = new UTF8Encoding(true).GetBytes(_importWriter.ToString());
    fs.Write(info, 0, info.Length);
}
extensions.ExecuteAllEnd();


void SetupWorkItems()
{
    for (int i = 0; i < _arguments.Length; i++)
    {
        switch (_arguments[i])
        {
            case "-h":
                Console.WriteLine("tmj2snes manpage");
                Console.WriteLine("-h          : this manpage");
                Console.WriteLine("-t!         : disables tileset conversion on .world or .tmj (world od map files)");
                Console.WriteLine("-t [file]   : convert tileset ");
                Console.WriteLine("-w [file]   : convert all maps in world file");
                Console.WriteLine("[file]      : convert map");
                Console.WriteLine("-a [name]   : add subfolder to data.asm");
                Console.WriteLine("maps, worlds and tileset can be cascaded");
                break;

            case "-t!": //map no tileset in world or map
                _disableTileset = true;
                break;

            case "-t":  //only tileset
                WorkItems.Add(new(eConvertMode.Tileset, GetFileFromArgs(i + 1)));
                i++;
                break;

            case "-w":  //world
                WorkItems.Add(new(eConvertMode.World, GetFileFromArgs(i + 1)));
                i++;
                break;

            case "-a":  //datasm subfolder
                _asmdir = GetFileFromArgs(i + 1);
                i++;
                break;

            default:    //map
                WorkItems.Add(new(eConvertMode.Map, GetFileFromArgs(i)));
                break;
        }
    }
}
void ConvertWorkItems()
{
    _dataWriter.AppendLine($"; section tiled nr {_SectionCounter}");
    _dataWriter.AppendLine($".section \".mapsection{_SectionCounter}\" superfree");
    foreach (var witem in WorkItems)
    {
        _file = witem.Item2;
        switch (witem.Item1)
        {
            case eConvertMode.Tileset:

                _importWriter.AppendLine($"//-----{_file}-----------");
                var ts = ExtractTilesetPropsFromFile(_file);
                ConvertTileset_T16(_file, ts);
                ConvertTileset_B16(_file, ts);
                Console.WriteLine($"Convert Tileset {_file}");
                _importWriter.AppendLine();
                break;

            case eConvertMode.World:
                var world = FileHandler.LoadFile<World>(_file);
                if (world == null) return;
                var path = new FileInfo(_file).Directory!.FullName;
                Console.WriteLine($"Convert World {_file}");
                foreach (var item in world.Maps)
                {
                    _importWriter.AppendLine($"//-----{item.FileName}-----------");
                    ConvertMap(Path.Combine(path, item.FileName), world);
                    Console.WriteLine($"Convert Map {item.FileName}");
                    _importWriter.AppendLine();
                }
                break;

            case eConvertMode.Map:
                _importWriter.AppendLine($"//-----{_file}-----------");
                ConvertMap(Path.Combine(_file), null);
                Console.WriteLine($"Convert Map {_file}");
                _importWriter.AppendLine();
                break;
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
void ConvertTileset_T16(string file, Tileset? tileset)
{
    if (tileset == null) return;
    if (!File.Exists(file)) return;
    var tName = Path.GetFileNameWithoutExtension(file);
    var path = new FileInfo(file).Directory!.FullName;
    using (FileStream t16Stream = new(Path.Combine(path, $"{tName}.t16"), FileMode.Create))
    {
        for (int i = 0; i < tileset.Tilecount; i++)
        {
            var res = tileset.Tiles[i].Id & 0x03FF;
            res |= tilesetbuffer[i].priority > 0 ? 0x2000 : 0x0000;
            res |= Math.Clamp(tilesetbuffer[i].palette, (ushort)0, (ushort)7) << 10;
            PutWord((ushort)res, t16Stream);
        }
    }
    WriteDataASM(tName, "tiledef_", "t16", path);
    WriteImports(tName, "tiledef_");
}
void ConvertTileset_B16(string file, Tileset? tileset)
{
    if (tileset == null) return;
    if (!File.Exists(file)) return;
    var bName = Path.GetFileNameWithoutExtension(file);

    var path = new FileInfo(file).Directory!.FullName;
    using (FileStream b16Stream = new(Path.Combine(path, $"{bName}.b16"), FileMode.Create))
    {
        for (int i = 0; i < tileset.Tilecount; i++)
        {
            PutWord(tilesetbuffer[i].attribute, b16Stream);
        }
    }
    WriteDataASM(bName, "tileatt_", "b16", path);
    WriteImports(bName, "tileatt_");
}

void ConvertMap(string file, World? world )
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
    if (!_disableTileset)
    {
        var ts = map.Tilesets[0];
        if (!string.IsNullOrEmpty(ts.Source))
        {
            var tsPath = Path.Combine(path, ts.Source);
            ts = ExtractTilesetPropsFromFile(tsPath);
        }
        ConvertTileset_T16(file, ts);
        ConvertTileset_B16(file, ts);
        _dataWriter.AppendLine();
    }
    int objectLayerCount = 0;
    int regionLayerCount = 0;
    extensions.ExecuteMapScript(path, mName, world);

    foreach (var layer in map.Layers)
    {
        if (layer.Type == "tilelayer")
        {
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
                        td = td == "0" ? "1" : td; //prevent going negative

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
            WriteDataASM(layer.Name, "", "m16", path);
            WriteImports(layer.Name, "");
        }
        else if (layer.Name.StartsWith("Regions") && layer.Type == "objectgroup")
        {
            if (layer.Objects.Count > N_REGIONS)
            {
                throw new ArgumentOutOfRangeException($"tmj2snes: error to many regions (max {N_REGIONS}) ({layer.Objects.Count})");
            }
            var rName = mName + layer.Name.Replace("Regions", "");
            using (FileStream objStream = new(Path.Combine(path, $"{rName}.r16"), FileMode.Create))
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
            WriteDataASM(rName, "reg_", "r16", path);
            WriteImports(rName, "reg_");
            regionLayerCount++;
        }
        else if (layer.Name.StartsWith("Entities") && layer.Type == "objectgroup")
        {
            if (layer.Objects.Count > N_OBJECTS)
            {
                throw new ArgumentOutOfRangeException($"tmj2snes: error to many objects (max {N_OBJECTS}) ({layer.Objects.Count})");
            }
            var oName = mName + layer.Name.Replace("Entities", "");
            using (FileStream objStream = new(Path.Combine(path, $"{oName}.o16"), FileMode.Create))
            {
                foreach (var obj in layer.Objects)
                {
                    pvsneslib_object_t pvobj = new()
                    {
                        x = Convert.ToUInt16(obj.X),
                        y = Convert.ToUInt16(obj.Y),
                        type = Convert.ToUInt16(obj.Type),
                        minx = 0,
                        maxx = 0
                    };

                    foreach (var prop in obj.Properties)
                    {
                        if (prop.Name == "minx")
                            pvobj.minx = ConvertTo16BitNumber(prop, mName);
                        if (prop.Name == "maxx")
                            pvobj.maxx = ConvertTo16BitNumber(prop, mName);
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
            WriteDataASM(oName, "obj_", "o16", path);
            WriteImports(oName, "obj_");
            objectLayerCount++;
        }
    }
 
}
ushort ConvertTo16BitNumber(Property input, string mapname)
{
    try
    {
#pragma warning disable IDE0079 // Bullshit warning
#pragma warning disable SYSLIB1045 //
        var res1 = new Regex(@"\b0x[0-9A-Fa-f]+\b").Match(input.Value.Trim());

        if (res1.Success)
        {
            var hv1 = res1.Value[2..];
            if (ushort.TryParse(hv1, System.Globalization.NumberStyles.HexNumber, null, out ushort result))
                return result;
        }
        var res2 = new Regex(@"\b0b[01]{16}\b").Match(input.Value.Trim());
        if (res2.Success)
        {
            var hv2 = res2.Value[2..];
            if (ushort.TryParse(hv2, System.Globalization.NumberStyles.HexNumber, null, out ushort result))
                return result;
        }
        var res3 = new Regex(@"\b0b[01]{8}\b").Match(input.Value.Trim());
        if (res3.Success)
        {
            var hv3 = res3.Value[2..];
            if (ushort.TryParse(hv3, System.Globalization.NumberStyles.HexNumber, null, out ushort result))
                return result;
        }
#pragma warning restore SYSLIB1045 // 
#pragma warning restore IDE0079 //
        return Convert.ToUInt16(input.Value);
    }
    catch (Exception)
    {
        Console.WriteLine($"error: {input.Value} on property {input.Name} in {mapname} is not valid");
        Environment.Exit(11);
    }
    return 0;

}
string GetFileFromArgs(int index)
{
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _arguments.Length);
    return _arguments[index];
}

byte HI_BYTE(ushort n) => (byte)((n >> 8) & 0x00ff); // extracts the hi-byte of a word
byte LOW_BYTE(ushort n) => (byte)(n & 0x00ff);      // extracts the low-byte of a word
void PutWord(ushort data, Stream fp)
{
    fp.WriteByte(LOW_BYTE(data));
    fp.WriteByte(HI_BYTE(data));
} // end of PutWord



void WriteDataASM(string filenname, string prefix, string suffix, string path)
{
    var file = $"{filenname}.{suffix}";
    var fi = new FileInfo(Path.Combine(path, file));
    if (_currentBankCounter + fi.Length >= ushort.MaxValue >> 1)
    {
        _dataWriter.AppendLine();
        _dataWriter.AppendLine($".ends ; .mapsection{_SectionCounter}, sectionsize {_currentBankCounter}/{ushort.MaxValue >> 1}");
        _dataWriter.AppendLine();
        _currentBankCounter = 0;
        _SectionCounter++;
        _dataWriter.AppendLine($"; section tiled nr {_SectionCounter}");
        _dataWriter.AppendLine($".section \".mapsection{_SectionCounter}\" superfree");
        _dataWriter.AppendLine();
    }

    _currentBankCounter += fi.Length;
    _totalBankCounter += fi.Length;

    var label = $"{prefix}{filenname}";
    _dataSizeExports.Add( label );
    _dataWriter.AppendLine();
    _dataWriter.AppendLine($"{label}:");
    _dataWriter.AppendLine($".incbin \"{Path.Combine(_asmdir, file)}\" FSIZE {label}_size\t;{fi.Length} bytes");
}
void WriteImports(string filenname, string prefix)
{
    _importWriter.AppendLine($"extern char {prefix}{filenname};");
}