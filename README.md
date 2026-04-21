# tmj2snes for PVSNESLIB

#features
- convert worlds, maps, and tilesets (Important! Only json based worlds, maps, or tilesets are supported!)- 
- able to extract nested and external tilesets in maps 
- creates a data.asm file (now with bank separation)
- create a exports.h header file for easier integration
- bake palette and priority direct into map if the tilelayer name starts with "bake"
- lua suppor 

#command line arguments :
- [file]      : convert a map
- -h          : manpage
- -t!         : disables tileset conversion on .world and .tmj (world and map files)
- -t [file]   : convert tileset 
- -w [file]   : convert all maps in the world file
- -a [name]   : add a subfolder to each line in data.asm
Conversion of maps, worlds, and tilesets can be cascaded

#example:
- tmj2snes -t! -w "C:\snesproject\my.world" -t "C:\snesproject\tileset.tsj" -a "maps/"

#this example creates all maps in "my.world" without the tileset and a separate tileset.
#For each "incbin:" entry the "maps/" will added 
- E.g.: "incbin: maps/tileset.t16" 

#lua extensions
1. create a subfolder with name "extensions"
2. create lua files with the following methods:
  * function runBegin()
    + runs once per world
    + for headerstuff
    + return string or table
    + keywords:
      - print("string")
    + return value table or string   
  * function runMap()
    + runs once per map in the world
    + for map relevant data
    + keywords
      - print("string")
      - basepath -> contains the absolute path of the maps folder
      - mapname -> mapname without ending
      - world -> world json data
    + return value table or string      
  * function runEnd(content)
    + runs once per world
    + its purpose is for saving data
    + keywords
      - print("string")
    + parameter content -> provides all the collected strings/tables from  runBegin() and runMap()





