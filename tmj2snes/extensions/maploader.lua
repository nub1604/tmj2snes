local cmn = require ("common") -- Load the JSON library


local mapNumber = 0
local sx, sy = 0, 0


config = {
    appName = "maploader",
    description = "codegenerator for maploader",
    filePath = "src/manager/code/map/",
    fileName = "map.loader.auto.c",
    version = 0.1
}

function runBegin()
    if(debugLua) then
        print("Running " .. config.appName)
    end

    local startdefs  = {}
    table.insert(startdefs, "#include \"map.loader.h\"")
    table.insert(startdefs, "void LoadMapFromRegion(u16 mapid, u16 x, u16 y)")
    table.insert(startdefs, "{")
    table.insert(startdefs, "\tIsMapBufferEnabled = 0;")
    table.insert(startdefs, "\tswitch (mapid)")
    table.insert(startdefs, "\t{")
	return startdefs
end

function runEnd(content)

    content = content .. "\t}\n}"
    local savepath = cmn.join(config.filePath, config.fileName)
    if(debugLua) then
        print("Sava fila " .. savepath)
    end
    local file = io.open(savepath, "w")
    file:write(content)
    file:close()

end

function runMap()
    local rTable  = {}
    local mapFile = cmn.join(mapname, ".tmj")
    -- print("Processing 1" .. mapFile)
    local sourceMapPath = cmn.join(basepath,"\\",mapFile)
    local sourceMap = cmn.decodeJsonFile(sourceMapPath)
     if (not sourceMap) then
       --print("Error: unable to decodeJsonFile " .. sourceMapPath)
       return "";
    end




    local tilesetname = ""
    if( sourceMap.tilesets[1]) then
        local  tsSource  = sourceMap.tilesets[1].source
        tilesetname = tsSource:gsub("%.%w+$", "")
    end



    local tilelayers = getJsonLayerByType(sourceMap, "tilelayer")
    local ifActive = 0
    local mapNumber = extractNumber(mapname);
    if sourceMap.properties  then
        local altnum = getJsonPropByName(sourceMap.properties, "num");
        if (altnum) then
            mapNumber = altnum
        end
    end
    table.insert(rTable,"\tcase " .. mapNumber .. ":")


    for _, layer in ipairs(tilelayers) do
        local lname = layer.name            -- BG1_xx_xx
        local lprops = layer.properties
        if(lprops) then

            -- insert If statements
            local ifstate = getJsonPropByName(lprops, "if");
            if(ifstate) then
                table.insert(rTable,"\t\tif(" .. ifstate .. ")")
                table.insert(rTable,"\t\t{")
                ifActive = 1;
            end

            local elseifstate = getJsonPropByName(lprops, "elseif");
            if(elseifstate) then
                table.insert(rTable,"\t\telse if(" .. elseifstate .. ")")
                table.insert(rTable,"\t\t{")
                ifActive = 1;
            end

            local elsestate = getJsonPropContainsName(lprops, "else");
            if(elsestate) then
                table.insert(rTable,"\t\telse")
                table.insert(rTable,"\t\t{")
                ifActive = 1;
            end

            local tabs = "\t\t"
            if(ifActive) then
                  tabs = "\t\t\t"
            end

            -- handle map load
            local load = getJsonPropByName(lprops, "Load");
            if(load) then
                if(load =="village1") then
                     table.insert(rTable,tabs .. "LoadVillageInner(x, y, (u8 *)&" .. lname ..", (char *)&obj_" .. mapname .. ", &reg_"..mapname..");")
                elseif (load == "village1b") then
                     table.insert(rTable,tabs .. "LoadVillageInnerBuffered(x, y, (u8 *)&" .. lname ..", (char *)&obj_" .. mapname .. ", &reg_"..mapname..", (u16)&" .. lname .."_size);")
                end
            else
            -- else handle seperate map config
                local BG = getJsonPropByName(lprops, "BG");
                if (BG) then
                    table.insert(rTable,tabs .. "LoadBG".. BG .."();")
                end
                local TS = getJsonPropByName(lprops, "TS");
                if (TS) then
                     table.insert(rTable,tabs .. "LoadTS".. TS .."();")
                end

                local isMapBuffered = getJsonPropContainsName(lprops, "Buffer");
                local MapCustom = getJsonPropByName(lprops, "MapCustom");
                if (isbuffered) then
                     table.insert(rTable,tabs .. "mmBufferedInitMap(x, y, (u8 *)&" .. lname ..", (u8 *)&tiledef_" .. tilesetname .. ", (u8 *)&tileatt_" .. tilesetname .. ", (char *)&obj_" .. mapname .. ", (u16)&" .. lname .."_size);")
                elseif (MapCustom) then
                    table.insert(rTable,tabs .. MapCustom)
                else
                     table.insert(rTable,tabs .. "mmDefaultInitMap(x, y, (u8 *)&" .. lname .. ", (u8 *)&tiledef_" .. tilesetname .. ", (u8 *)&tileatt_" .. tilesetname .. ", (char *)&obj_" .. mapname .. ");")
                end



                local regionLayer = getJsonLayerByName(sourceMap,"Regions")
                if (regionLayer) then
                     table.insert(rTable,tabs .. "loadRegions(&reg_"..  mapname..");")
                end

                local theme = getJsonPropByName(lprops,"Theme")
                if (theme) then
                     local splitted =  cmn.split(theme, ",")
                     local song = splitted[1]
                     local volume  = splitted[2] or 100
                     table.insert(rTable,tabs .. "loadTheme(" .. song..", " .. volume ..");")
                end
            end
            if(ifActive == 1) then
                table.insert(rTable, "\t\t}")
                ifActive = 0
            end
        end
    end


    table.insert(rTable,"\t\tbreak;");
    return rTable
end

function getJsonLayerByType(map, layertype)
    local layersmatches = {}
    for _, layer in ipairs(map.layers) do
        if layer.type == layertype then
            table.insert(layersmatches, layer)
        end
    end
    return layersmatches
end

function getJsonLayerByName(map, layerName)
    for _, layer in ipairs(map.layers) do
        if layer.name == layerName then
            return layer
        end
    end
    return nil
end

function getJsonObjectsByTrigger(layer, trigger)
    if not layer or not layer.objects then
        return {} -- Return an empty table if the layer is invalid
    end
    local matches = {}
    for _, obj in ipairs(layer.objects) do
        if(obj and obj.properties) then
            for _, prop in ipairs(obj.properties) do
                if(prop) then
                    if prop.name == trigger then
                        table.insert(matches, obj)
                        break
                    end
                end
            end
        end
    end
    return matches
end

function getTriggerProperty(obj, trigger)
    for _, prop in ipairs(obj.properties) do
        if prop.name == trigger then
            return prop.value
        end
    end
    return nil
end

function  getWorldPosition(world, filename)
    for index = 1, world.Maps.Length -1 do
        local map =  world.Maps[index]
        if map.FileName == filename then
            print(map.FileName .. " - " .. filename)
            return map.X,map.Y
        end
    end
end

function getJsonPropContainsName(props, name)
    for _, prop in ipairs(props) do
        if prop.name == name then
             return true
        end
    end
    return nil
end

function getJsonPropByName(props, name)
    for _, prop in ipairs(props) do
        if prop.name == name then
             return prop.value
        end
    end
    return nil
end



function extractNumber(str)
    -- Remove file extension if present
    str = str:match("^(.-)%.") or str
    -- Extract the last two numbers using pattern matching
    local num1, num2 = str:match("_(%d+)_(%d+)$")
    if num1 and num2 then
        return tonumber(num1 .. num2)
    end
    return nil -- Return nil if pattern doesn't match
end
