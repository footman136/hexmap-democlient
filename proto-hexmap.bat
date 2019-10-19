@echo off

set WorkPath=..\hexmap-proto-generator\
set SrcPath=..\hexmap-proto-generator\proto
rem 这个路径要从proto-generator-template这个bat所在位置寻找
set OutputPathClient=..\hexmap-democlient\Assets\Scripts\Network\protobuf
set OutputPathLobbyServer=..\hexmap-lobbyserver\Assets\Scripts\Network\protobuf
set OutputPathRoomServer=..\hexmap-roomserver\Assets\Scripts\Network\protobuf

@echo -------------
@echo %SrcPath%
@echo %OutputPathClient%
@echo -------------
@echo Generating to client ...
call %WorkPath%proto-generator-template %SrcPath% %OutputPathClient%

@echo -------------
@echo Generating to lobby server ...
call %WorkPath%proto-generator-template %SrcPath% %OutputPathLobbyServer%
@echo -------------

@echo -------------
@echo Generating to room server ...
call %WorkPath%proto-generator-template %SrcPath% %OutputPathRoomServer%
@echo -------------

pause
