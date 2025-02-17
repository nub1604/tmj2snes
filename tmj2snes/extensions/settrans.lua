local json = require ("dkjson") -- Load the JSON library
local cmn = require ("common") -- Load the JSON library


config = {
    appName = "settrans",
    description = "creates maptransition table tiled objects named \"Regions\"",
    version = 0.1
}
function runBegin()
    if(debugLua) then
        
    end
	return "testStart"    
end
function runEnd()
	return "testEnd"    
end

function runMap()
    local trigger = "settrans"
    local sourceMapPath = cmn.join(basepath,"\\", mapname, ".tmj")
    local sourceMap = decodeJsonFile(sourceMapPath)
     if (not sourceMap) then
       --print("Error: unable to decodeJsonFile " .. sourceMapPath)
       return "";
    end
    local regionsLayer = getJsonLayerByName(sourceMap,"Regions")

    -- Check if the layer exists
    if (not regionsLayer) then
        --print("Error: no regionsLayer in " .. sourceMapPath)
       return "";
    end

 
    local objects = getJsonObjectsByTrigger(regionsLayer,trigger);
    if (#objects == 0) then
        return "";
    end
    return analyseObject(objects, trigger)	
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

function split(inputstr, sep)
    local trimmed = string.gsub(inputstr, "%s+", "")
    local t = {}
    for str in string.gmatch(trimmed, "([^"..sep.."]+)") do
        table.insert(t, str)
    end
    return t
end

function getTriggerProperty(obj, trigger)
    for _, prop in ipairs(obj.properties) do
        if prop.name == trigger then
            return prop.value
        end
    end
    return nil
end

function decodeJsonFile(filename)
    local file = io.open(filename, "r")
    if not file then
        print("Error: Could not open file " .. filename)
        return nil
    end
    local content = file:read("*a")
    file:close()
    local obj, _, err = json.decode (content, 1, nil)
    if err then
        print("JSON Decode Error: " .. err)
    else
        return obj
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



function analyseObject(objects, trigger)
    local resultStrings = {} 
    for _,obj in ipairs(objects) do
        local value = getTriggerProperty(obj, trigger)
        if(value) then 
            local vs = split(value, ",")
            if(#vs > 2) then
                local targetMap  = vs[1];
                local type  = vs[2];
                local direction = vs[3]; 
                local offset = vs[4] or 0;

                local targetMapPath = cmn.join(basepath,"\\", targetMap, ".tmj")
                local tm = decodeJsonFile(targetMapPath)
                local to = getJsonObjectByType(tm,type)
                
                if(to) then
       
                    local str = ""
                    cmn.switch(direction)
                        .case("l", function() str = cmn.join(".db ", to.x -1, ",", 0, ",", "RF_LEFT")end)
                        .case("r", function() str = cmn.join(".db ", to.x + to.width+1, ",", 0, ",", "RF_RIGHT")end)
                        .case("u", function() str = cmn.join(".db ", 0, ",", tm.height -to.height -1, ",", "RF_UP")end)
                        .case("d", function() str = cmn.join(".db ", 0, ",", to.y +to.height +1, ",", "RF_DOWN")end)
                        .case("x", function() str = cmn.join(".db ", to.X, ",", to.Y, ",", "RF_DIRECT")end)
                        .process()
                    if (debugLua) then
                        print("settrans: " .. str)
                    end
                    table.insert(resultStrings, str)
                end
            else
                error(trigger .. "expects 3 value in " .. value)
            end
        end
    end
    return resultStrings
end