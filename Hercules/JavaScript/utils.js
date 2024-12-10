var utils =
{
    visit:
        function (data, callback, schema) {
            if (!schema)
                schema = hercules.db.get('schema');
            var descriptor = { kind: "record", optional: false, name: schema.document_type };
            return this.visit_internal("", data, schema, descriptor, callback);
        },

    getCustomFields:
        function (data) {
            var schema = hercules.db.get('schema');
            var customType = schema.custom_types[schema.document_type];
            if ((customType.kind == "variant") && data.hasOwnProperty(customType.tag)) {
                var tag = data[customType.tag];
                if (customType.children.hasOwnProperty(tag)) {
                    var customType = schema.custom_types[customType.children[tag]];
                }
            }
            var customFields = {};
            for (var field in data) {
                if (field == "_rev" || field == "_id" || field == "hercules_metadata")
                    continue;

                var fieldFound = false;
                var type = customType;
                while (type) {
                    if (type.fields.hasOwnProperty(field)) {
                        fieldFound = true;
                        break;
                    }
                    if (type.parent) {
                        type = schema.custom_types[type.parent];
                    } else {
                        type = null;
                    }
                }

                if (!fieldFound)
                    customFields[field] = data[field];
            }

            return customFields;
        },

    append_path:
        function (path, key) {
            if (path == "")
                return key;
            else
                return path + "." + key;
        },

    visit_internal:
        function (path, initialData, schema, descriptor, callback) {
            if (initialData == null)
                return null;

            var data = callback(initialData, path, descriptor);

            if (data === undefined)
                data = initialData;

            if (data == null)
                return null;

            switch (descriptor.kind) {
                case "dict":
                    for (var key in data) {
                        data[key] = this.visit_internal(this.append_path(path, key), data[key], schema, descriptor.value, callback);
                    }
                    break;

                case "list":
                    for (var i in data) {
                        data[i] = this.visit_internal(path + "[" + i + "]", data[i], schema, descriptor.element, callback);
                    }
                    break;

                case "record":
                    {
                        var customType = schema.custom_types[descriptor.name];
                        if ((customType.kind == "variant") && data.hasOwnProperty(customType.tag)) {
                            var tag = data[customType.tag];
                            if (customType.children.hasOwnProperty(tag)) {
                                var customType = schema.custom_types[customType.children[tag]];
                            }
                        }
                        while (customType) {
                            for (var field in customType.fields) {
                                if (data.hasOwnProperty(field)) {
                                    data[field] = this.visit_internal(this.append_path(path, field), data[field], schema, customType.fields[field], callback);
                                }
                            }
                            if (customType.parent) {
                                customType = schema.custom_types[customType.parent];
                            } else {
                                customType = null;
                            }
                        }
                    }
                    break;
            }
            return data;
        }
};
