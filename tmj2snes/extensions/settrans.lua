local json = require ("dkjson") -- Load the JSON library
   if(debugLua) then
end
local trigger = "settrans"

function GetObjectsByTrigger(layer, trigger)
    if not layer or not layer.Objects then
        return {} -- Return an empty table if the layer is invalid
    end
    local matches = {}
    for o = 0, layer.Objects.Count - 1 do
        local obj = layer.Objects[o]
        for i = 0, obj.Properties.Count - 1 do
        local prop = obj.Properties[i]
            if prop.Name == trigger then
                table.insert(matches, obj)
                break
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

function GetTriggerProperty(obj, trigger)
    for i = 0, obj.Properties.Count - 1 do
        local prop = obj.Properties[i]
        if prop.Name == trigger then
            return prop.Value
        end
    end
    return nil
end

function DecodeJsonFile(filename)
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

function join(...)
    local args = {...} -- Get all arguments
    return table.concat(args, "") -- Join without spaces
end
function dump (tbl, indent)
    if not indent then indent = 0 end
    for k, v in pairs(tbl) do
        formatting = string.rep("  ", indent) .. k .. ": "
        if type(v) == "table" then
            print(formatting)
            dump(v, indent+1)
        elseif type(v) == 'boolean' then
            print(formatting .. tostring(v))      
        else
            print(formatting .. v)
        end
    end
end

function GetJsonObjectByType(jsonmap, type)
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


function AnalyseObject(objects, trigger)
    local resultStrings = {} 
    for _,obj in ipairs(objects) do
        local value = GetTriggerProperty(obj, trigger)
        if(value) then
            print(value)
            local vs = split(value, ",")
            if(#vs > 2) then
                local targetMap  = vs[1];
                local type  = vs[2];
                local direction = vs[3];    
                local targetMapPath = join(basepath,"\\", targetMap, ".tmj")
                local tm = DecodeJsonFile(targetMapPath)
                local to = GetJsonObjectByType(tm,type)
                
                if(to) then
                --Todo: Create Output                    

                    local str = join(".db ", obj.X, ",", obj.Y, ",", obj.Width, ",", obj.Height, ",", to.x, ",", to.y, ",", to.width, ",", to.height, ",", direction)
                    table.insert(t, str)
                    resultStrings.i
                    local x = 1

                    print("to.x: " .. to.x)
                    print("to.y: " .. to.y)
                    print("to.width: " .. to.width)
                    print("to.height: " .. to.height)
                end
            else
                 error(trigger .. "expects 3 value in " .. value)
            end
        end
    end
end




function GetLayerByName(layerName)
    for _, layer in ipairs(map.Layers) do
        if layer.Name == layerName then
            return layer 
        end
    end
    return nil
end

local regionsLayer = GetLayerByName("Regions")

-- Check if the layer exists
if (not regionsLayer) then
   return "";
end

local objects = GetObjectsByTrigger(regionsLayer,trigger);
if (#objects == 0) then
    return "";
end
AnalyseObject(objects, trigger)