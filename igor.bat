Tools\igorc.exe -d -v -cs -set json.library=artplant -set nullable -o Hercules\Core\Schema Hercules\Core\Schema\diagram_schema.igor Hercules\Core\Schema\schema.igor
Tools\igorc.exe -d -v -schema -o Hercules\Resources\Schema -output-file schema.json Hercules\Core\Schema\schema.igor
Tools\igorc.exe -d -v -schema -o Hercules\Resources\Schema -output-file diagram_schema.json Hercules\Core\Schema\diagram_schema.igor
Tools\igorc.exe -d -v -cs -set json.library=artplant -set nullable -o Hercules\Forms\CustomTypes Hercules\Forms\CustomTypes\editors.igor
Tools\igorc.exe -d -v -schema -o Hercules\Resources\Schema -output-file editors.json Hercules\Forms\CustomTypes\editors.igor
Tools\igorc.exe -d -v -cs -set json.library=artplant -set nullable -o Hercules\Scripting\Schema Hercules\Scripting\Schema\scripts.igor
Tools\igorc.exe -d -v -schema -o Hercules\Resources\Schema -output-file scripts.json Hercules\Scripting\Schema\scripts.igor
Tools\igorc.exe -d -v -cs -set json.library=artplant -set nullable -o Hercules\Core\CouchDB Hercules\Core\CouchDB\*.igor
REM igorc.exe -v -cs -target-version 8.0 -set nullable -o Hercules\Core\Unreal Hercules\Core\Unreal\*.igor

if errorlevel 1 pause