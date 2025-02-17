local common = {}

function common.join(...)
    local args = {...} -- Get all arguments
    return table.concat(args, "") -- Join without spaces
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

return common -- Return the module