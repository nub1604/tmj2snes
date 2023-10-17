# tmj2snes for PVSNESLIB

#teatures
- convert world, maps, and tilesets
- able to extract nested and external tilesets in maps 
- creates a data.asm file (no bank separation)

#command line arguments :
- [file]      : convert a map
- -h          : manpage
- -t!         : disables tileset conversion on .world and .tmj (world and map files)
- -t [file]   : convert tileset 
- -w [file]   : convert all maps in world file
- -a [name]   : add subfolder to each line in data.asm
Conversion of maps, worlds, and tilesets can be cascaded

#example:
- tmj2snes -t! -w "C:\snesproject\my.world" -t "C:\snesproject\tileset.tsj" -a "maps/"

#this example creates all maps in my.world without the tileset and a separate tileset.
#For each "incbin:" entry the "maps/" will added 
- E.g.: "incbin: maps/tileset.t16" 
