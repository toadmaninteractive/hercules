.. meta::
   :http-equiv=X-UA-Compatible: IE=Edge

********************************
Hercules Schema Meta Attributes
********************************

With schema meta attributes (used with Igor types), you can control Hercules editor’s behavior and appearance. Such attributes are not validated by Igor Compiler, so Hercules gets to introduce new meta attributes without having to alter or update Igor Compiler.

**Example:**

.. code-block:: igor

	[schema min=0 max=1 meta=(step=0.1 slider.ticks)]
	float value;

This is a list of metadata values that Hercules understands and processes:


+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| Attribute              | Type           | Target         | Description                                       | Default        |
+========================+================+================+===================================================+================+
| compact                | **bool**       | | record       | | Enables compact view for the record             | ``false``      |
|                        |                | | record field |                                                   |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| step                   | **float**      | | type         | | Value change step for float numbers             |                |
|                        |                | | record field | | (schema min and max should be also set)         |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| slider.ticks           | **bool**       | | type         | | Display float slider ticks                      | ``false``      |
|                        |                | | record field | | (step should be also set)                       |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| format                 | **string**     | | type         | String format, e.g. 0.## or 0.00                  |                |
|                        |                | | record field |                                                   |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| record_caption         | **bool**       | record field   | Displays this field value as the record caption   | ``false``      |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| record_enabled         | **bool**       | record field   | | Displays this field as a checkbox in the        | ``false``      |
|                        |                |                | | record header                                   |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| unreal_class_path      | **bool**       | | type         | | Displays this field as a checkbox in the        | ``false``      |
|                        |                | | record field | | record header                                   |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| atomic                 | **bool**       | type           | | The type is atomic in linked documents context. | ``false``      |
|                        |                |                | | See :ref:`Atomic values <atomic>`               |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| key                    | **bool**       | record field   | | The field can be used to identify a record list | ``false``      |
|                        |                |                | | item. See                                       |                |
|                        |                |                | | :ref:`List of records identified by keys <list>`|                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| reference.id           | **string**     | | type         | | The value is a referenced value.                |                |
|                        |                | | record field | | See :ref:`Local references <local-references>`  |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| reference.source       | **bool**       | | type         | | The value is a reference source.                | ``false``      |
|                        |                | | record field | | See :ref:`Local references <local-references>`  |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| reference.unique       | **bool**       | | type         | | The reference source must be unique.            | ``false``      |
|                        |                | | record field | | See :ref:`Local references <local-references>`  |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| reference.validate     | **bool**       | | type         | | Reference should be validated by Hercules.      | ``true``       |
|                        |                | | record field | | See :ref:`Local references <local-references>`  |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| caption_path           | **string**     | record type    | | Path to the document record caption used as     |                |
|                        |                |                | | additional info in document key pickers         |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| image_path             | **string**     | record type    | Path to the document image used in tile view      |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| preview                | **bool**       | | type         | Show preview image                                | ``false``      |
|                        |                | | record field |                                                   |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| preview.width          | **int**        | | type         | Preview image width                               | 200            |
|                        |                | | record field |                                                   |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
| preview.height         | **int**        | | type         | Preview image height                              | 200            |
|                        |                | | record field |                                                   |                |
+------------------------+----------------+----------------+---------------------------------------------------+----------------+
