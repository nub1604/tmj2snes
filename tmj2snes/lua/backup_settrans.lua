local cmn = require ("common") -- Load the JSON library


local mapNumber = 0
local sx, sy = 0, 0


config = {
    appName = "settrans",
    description = "creates maptransition table tiled objects named \"Regions\"",
    filePath = "../src/data/",
    fileName = "settransa.asm",
    version = 0.1
}

function runBegin()
    if(debugLua) then
        print("Running " .. appName)
    end
	return ""    
end

function runEnd(content)

    content = content .. "\n .ends"
    local savepath = cmn.join(config.filePath, config.fileName)
    if(debugLua) then
        print("Sava fila " .. savepath)
    end
    local file = io.open(savepath, "w")
    file:write(content)
    file:close()

end

function runMap()
    local trigger = "settrans"
    local mapFile = cmn.join( mapname, ".tmj")

    local sourceMapPath = cmn.join(basepath,"\\",mapFile)
    local sourceMap = cmn.decodeJsonFile(sourceMapPath)
     if (not sourceMap) then
       --print("Error: unable to decodeJsonFile " .. sourceMapPath)
       return "";
    end

     mapNumber = extractNumber(mapname);
    -- Check if the regionlayer exists
    local regionsLayer = getJsonLayerByName(sourceMap,"Regions")


    if (not regionsLayer) then
        --print("Error: no regionsLayer in " .. sourceMapPath)
       return "";
    end

 
    local objects = getJsonObjectsByTrigger(regionsLayer,trigger);
    if (#objects == 0) then
        return "";
    end
    
    sx, sy  = getWorldPosition(world, mapFile)
    local result = analyseObject(objects, trigger)

    if(#result > 0) then
        table.insert(result, 1,".settrans_" .. mapname)
        table.insert(result, ".settrans_" .. mapname .. "_end")
        return result
    end
    return nil
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



function getJsonObjectByType(jsonmap, type)
    for _, layer in ipairs(jsonmap.layers) do
        if layer.name == "Regions" then
            for _, obj in ipairs(layer.objects) do
                if (obj.type == type) then
                    return obj
                end
            end
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

function analyseObject(objects, trigger)
    local resultStrings = {} 

    for _,obj in ipairs(objects) do

        local value = getTriggerProperty(obj, trigger)
        if(value) then 
            local vs = cmn.split(value, ",")
            if(#vs > 2) then
                local targetMap = vs[1];
                local type  = vs[2];
                local direction = vs[3]; 
                local offsetX = vs[4] or 0;
                local offsetY = vs[5] or 0;

                local targetMapFile = cmn.join(targetMap, ".tmj")
                local targetMapPath = cmn.join(basepath,"\\", targetMapFile)
                local tm = cmn.decodeJsonFile(targetMapPath)
                local to = getJsonObjectByType(tm,type)
                
                local ox, oy = getWorldPosition(world, targetMapFile)
               
                if (direction ~= "x") then
                    ox = sx - ox
                    oy = sy - oy
                end

                if(to) then
       
                    local str = cmn.join(".db ",obj.type, ",", mapNumber ,",")
                    cmn.switch(direction)
                    .case("l", function() str = cmn.join(str, offsetX + to.x - tm.width -1      , ",", offsetY + oy                      , ",", "RF_LEFT") end) 
                    .case("r", function() str = cmn.join(str, offsetX + to.x + to.width + oy + 1, ",", offsetY                           , ",", "RF_RIGHT") end)
                    .case("u", function() str = cmn.join(str, offsetX + ox                      , ",", offsetY + tm.height - to.height -8, ",", "RF_UP") end)
                    .case("d", function() str = cmn.join(str, offsetX + ox                      , ",", offsetY + to.y + to.height + 1    , ",", "RF_DOWN") end)
                    .case("x", function() str = cmn.join(str, offsetX + to.x                    , ",", offsetY + to.y                    , ",", "RF_DIRECT") end)
                    .process()
                    table.insert(resultStrings, str)
                end
            else
                error(trigger .. "expects 3 value in " .. value)
            end
        end
    end
    return resultStrings
end