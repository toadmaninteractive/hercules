.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

******************************
Hercules for Unreal Engine
******************************

Blueprint Class Path
--------------------

This example demonstrates the use of Blueprint references:

	.. code-block:: igor

		[schema meta=(unreal_class_path) path path.default="Content/Blueprints/Weapons"]
		string blueprint;

The ``unreal_class_path meta`` attribute enables special support for blueprint references.

You can use the **Copy Reference** command on the blueprint asset in Unreal Engine editor and paste the content in the blueprint field. The value gets converted automatically from ``Blueprint'/Game/Blueprints/Weapons/BP_Uzi.BP_Uzi'`` to ``/Game/Blueprints/Weapons/BP_Uzi.BP_Uzi_C``.

Additionally, you can use the file picker to pick an asset from the local project repository. With the optional ``path.default`` attribute, you can specify a relative path to the default folder. The value gets converted automatically from ``Content/Blueprints/Weapons/BP_Uzi.uasset`` to ``/Game/Blueprints/Weapons/BP_Uzi.BP_Uzi_C``.

This value can be used directly with ``FSoftClassPath`` or similar Unreal Engine classes.


