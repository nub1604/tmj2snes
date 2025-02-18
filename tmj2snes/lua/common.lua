local json = require ("dkjson") -- Load the JSON library
local common = {}

function common.join(...)
    local args = {...} -- Get all arguments 
    for i = 1, #args do
        if args[i] == nil then
            print("Error: Argument " .. i .. " is nil")
            for j = 1, i-1 do
                print("Args" .. j .. ": " .. args[j])
            end
        end
    end
    return table.concat(args, "") -- Join without spaces
end


function common.split(inputstr, sep)
    local trimmed = string.gsub(inputstr, "%s+", "")
    local t = {}
    for str in string.gmatch(trimmed, "([^"..sep.."]+)") do
        table.insert(t, str)
    end
    return t
end



function common.dump (tbl, indent)
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

function common.switch(element)
  local Table = {
    ["Value"] = element,
    ["DefaultFunction"] = nil,
    ["Functions"] = {}
  }
  
  Table.case = function(testElement, callback)
    Table.Functions[testElement] = callback
    return Table
  end
  
  Table.default = function(callback)
    Table.DefaultFunction = callback
    return Table
  end
  
  Table.process = function()
    local Case = Table.Functions[Table.Value]
    if Case then
      Case()
    elseif Table.DefaultFunction then
      Table.DefaultFunction()
    end
  end
  
  return Table
end


function common.decodeJsonFile(filename)
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


return common -- Return the module