Tools\igorc.exe -d -v -cs -target-version 8.0 -set nullable -o Hercules\Core\Schema Hercules\Core\Schema\diagram_schema.igor Hercules\Core\Schema\schema.igor
Tools\igorc.exe -d -v -schema -o Hercules\Resources\Schema -output-file schema.json Hercules\Core\Schema\schema.igor
Tools\igorc.exe -d -v -schema -o Hercules\Resources\Schema -output-file diagram_schema.json Hercules\Core\Schema\diagram_schema.igor
Tools\igorc.exe -d -v -cs -target-version 8.0 -set nullable -o Hercules\Forms\CustomTypes Hercules\Forms\CustomTypes\editors.igor
Tools\igorc.exe -d -v -schema -o Hercules\Resources\Schema -output-file editors.json Hercules\Forms\CustomTypes\editors.igor
Tools\igorc.exe -d -v -cs -target-version 8.0 -set nullable -o Hercules\Scripting\Schema Hercules\Scripting\Schema\scripts.igor
Tools\igorc.exe -d -v -schema -o Hercules\Resources\Schema -output-file scripts.json Hercules\Scripting\Schema\scripts.igor
Tools\igorc.exe -d -v -cs -target-version 8.0 -set nullable -o Hercules\Core\CouchDB Hercules\Core\CouchDB\*.igor
REM igorc.exe -v -cs -target-version 8.0 -set nullable -o Hercules\Core\Unreal Hercules\Core\Unreal\*.igor
Tools\igorc.exe -v -cs -target-version 8.0 -set nullable -o Hercules\Chronos Hercules\Chronos\*.igor
Tools\igorc.exe -v -cs -target-version 8.0 -set nullable -o Hercules\Analytics Hercules\Analytics\*.igor
Tools\igorc.exe -v -cs -target-version 8.0 -set nullable -o Hercules\Analytics\Scylla Hercules\Analytics\Scylla\*.igor
Tools\igorc.exe -d -v -schema -o Hercules\Analytics -output-file analytics.json -schema -set enabled -set root_type='AnalyticsEvent' Hercules\Analytics\analytics.igor

if errorlevel 1 pause