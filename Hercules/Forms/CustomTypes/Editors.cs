// Author: Igor compiler
// Compiler version: igorc 2.1.3
// DO NOT EDIT THIS FILE - it is machine generated

using System.Collections.Generic;

using JsonSerializer = Json.Serialization.JsonSerializer;

namespace Hercules.Forms.Schema.Custom
{
    public enum EditorType
    {
        Icon = 1,
        Curve = 2,
        Plot = 3,
        InteractiveMap = 4,
        Breadcrumbs = 5,
    }

    public enum EditorScope
    {
        Editor = 1,
    }

    public enum PlotType
    {
        Points = 1,
    }

    public abstract class Editor
    {
        public abstract EditorType EditorType { get; }

        public EditorScope Scope { get; set; } = EditorScope.Editor;

        protected Editor()
        {
        }
    }

    public sealed class EditorIcon : Editor
    {
        public override EditorType EditorType => EditorType.Icon;

        public string Atlas { get; set; } = "";

        public int IconWidth { get; set; }

        public int IconHeight { get; set; }

        public int Default { get; set; }
    }

    public sealed class EditorCurveKnot
    {
        public System.Windows.Point Position { get; }

        public double TangentIn { get; }

        public double TangentOut { get; }

        public EditorCurveKnot(System.Windows.Point position, double tangentIn, double tangentOut)
        {
            this.Position = position;
            this.TangentIn = tangentIn;
            this.TangentOut = tangentOut;
        }
    }

    public sealed class EditorCurveData : System.IEquatable<EditorCurveData?>
    {
        public IReadOnlyList<EditorCurveKnot> Knots { get; }

        public EditorCurveData(IReadOnlyList<EditorCurveKnot> knots)
        {
            if (knots == null)
                throw new System.ArgumentNullException(nameof(knots));
            this.Knots = knots;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Knots == null ? 0 : Knots.GetHashCode());
                return hash;
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EditorCurveData);
        }

        public bool Equals(EditorCurveData? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return System.Linq.Enumerable.SequenceEqual(Knots, other.Knots);
        }
    }

    public sealed class EditorCurvePreset
    {
        public string Name { get; }

        public EditorCurveData CurveData { get; set; }

        public EditorCurvePreset(string name, EditorCurveData curveData)
        {
            if (name == null)
                throw new System.ArgumentNullException(nameof(name));
            if (curveData == null)
                throw new System.ArgumentNullException(nameof(curveData));
            this.Name = name;
            this.CurveData = curveData;
        }
    }

    public sealed class EditorCurve : Editor
    {
        public override EditorType EditorType => EditorType.Curve;

        public System.Windows.Rect DefaultViewport { get; set; }

        public bool AutoScalePreviewX { get; set; } = false;

        public bool AutoScalePreviewY { get; set; } = false;

        public bool AutoScaleEditor { get; set; } = false;

        public float PreviewWidth { get; set; } = 200f;

        public float PreviewHeight { get; set; } = 200f;

        public string? AxisXLabel { get; set; }

        public string? AxisYLabel { get; set; }

        public List<EditorCurvePreset> Presets { get; set; } = new List<EditorCurvePreset>();
    }

    public sealed class EditorPlotData : System.IEquatable<EditorPlotData?>
    {
        public IReadOnlyList<System.Windows.Point> Points { get; }

        public EditorPlotData(IReadOnlyList<System.Windows.Point> points)
        {
            if (points == null)
                throw new System.ArgumentNullException(nameof(points));
            this.Points = points;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Points == null ? 0 : Points.GetHashCode());
                return hash;
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EditorPlotData);
        }

        public bool Equals(EditorPlotData? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return System.Linq.Enumerable.SequenceEqual(Points, other.Points);
        }
    }

    public sealed class EditorPlot : Editor
    {
        public override EditorType EditorType => EditorType.Plot;

        public System.Windows.Rect DefaultViewport { get; set; }

        public PlotType PlotType { get; set; }

        public bool AutoScalePreviewX { get; set; } = false;

        public bool AutoScalePreviewY { get; set; } = false;

        public bool AutoScaleEditor { get; set; } = false;

        public float PreviewWidth { get; set; } = 200f;

        public float PreviewHeight { get; set; } = 200f;
    }

    public sealed class EditorInteractiveMap : Editor
    {
        public override EditorType EditorType => EditorType.InteractiveMap;

        public string Title { get; set; } = "";
    }

    public sealed class BreadcrumbsData
    {
        public string Name { get; }

        public IReadOnlyList<BreadcrumbsData>? Items { get; }

        public BreadcrumbsData(string name, IReadOnlyList<BreadcrumbsData>? items = null)
        {
            if (name == null)
                throw new System.ArgumentNullException(nameof(name));

            this.Name = name;
            this.Items = items;
        }
    }

    public sealed class EditorBreadcrumbs : Editor
    {
        public override EditorType EditorType => EditorType.Breadcrumbs;

        public IReadOnlyList<BreadcrumbsData> Items { get; set; } = System.Array.Empty<BreadcrumbsData>();
    }

    public sealed class VectorJsonSerializer : Json.Serialization.IJsonSerializer<System.Windows.Point>
    {
        public static readonly VectorJsonSerializer Instance = new VectorJsonSerializer();

        public Json.ImmutableJson Serialize(System.Windows.Point value)
        {
            return new Json.JsonObject
            {
                ["x"] = JsonSerializer.Double.Serialize(value.X),
                ["y"] = JsonSerializer.Double.Serialize(value.Y)
            };
        }

        public System.Windows.Point Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var x = JsonSerializer.Double.Deserialize(json["x"]);
            var y = JsonSerializer.Double.Deserialize(json["y"]);
            return new System.Windows.Point(x, y);
        }
    }

    public sealed class RectJsonSerializer : Json.Serialization.IJsonSerializer<System.Windows.Rect>
    {
        public static readonly RectJsonSerializer Instance = new RectJsonSerializer();

        public Json.ImmutableJson Serialize(System.Windows.Rect value)
        {
            return new Json.JsonObject
            {
                ["x"] = JsonSerializer.Double.Serialize(value.X),
                ["y"] = JsonSerializer.Double.Serialize(value.Y),
                ["width"] = JsonSerializer.Double.Serialize(value.Width),
                ["height"] = JsonSerializer.Double.Serialize(value.Height)
            };
        }

        public System.Windows.Rect Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var x = JsonSerializer.Double.Deserialize(json["x"]);
            var y = JsonSerializer.Double.Deserialize(json["y"]);
            var width = JsonSerializer.Double.Deserialize(json["width"]);
            var height = JsonSerializer.Double.Deserialize(json["height"]);
            return new System.Windows.Rect(x, y, width, height);
        }
    }

    public sealed class EditorTypeJsonSerializer : Json.Serialization.IJsonSerializer<EditorType>, Json.Serialization.IJsonKeySerializer<EditorType>
    {
        public static readonly EditorTypeJsonSerializer Instance = new EditorTypeJsonSerializer();

        public Json.ImmutableJson Serialize(EditorType value)
        {
            return SerializeKey(value);
        }

        public EditorType Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            return DeserializeKey(json.AsString);
        }

        public string SerializeKey(EditorType value)
        {
            return value switch
            {
                EditorType.Icon => "icon",
                EditorType.Curve => "curve",
                EditorType.Plot => "plot",
                EditorType.InteractiveMap => "interactive_map",
                EditorType.Breadcrumbs => "breadcrumbs",
                _ => throw new System.ArgumentOutOfRangeException(nameof(value))
            };
        }

        public EditorType DeserializeKey(string jsonKey)
        {
            return jsonKey switch
            {
                "icon" => EditorType.Icon,
                "curve" => EditorType.Curve,
                "plot" => EditorType.Plot,
                "interactive_map" => EditorType.InteractiveMap,
                "breadcrumbs" => EditorType.Breadcrumbs,
                _ => throw new System.ArgumentOutOfRangeException(nameof(jsonKey))
            };
        }
    }

    public sealed class EditorScopeJsonSerializer : Json.Serialization.IJsonSerializer<EditorScope>, Json.Serialization.IJsonKeySerializer<EditorScope>
    {
        public static readonly EditorScopeJsonSerializer Instance = new EditorScopeJsonSerializer();

        public Json.ImmutableJson Serialize(EditorScope value)
        {
            return SerializeKey(value);
        }

        public EditorScope Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            return DeserializeKey(json.AsString);
        }

        public string SerializeKey(EditorScope value)
        {
            return value switch
            {
                EditorScope.Editor => "editor",
                _ => throw new System.ArgumentOutOfRangeException(nameof(value))
            };
        }

        public EditorScope DeserializeKey(string jsonKey)
        {
            return jsonKey switch
            {
                "editor" => EditorScope.Editor,
                _ => throw new System.ArgumentOutOfRangeException(nameof(jsonKey))
            };
        }
    }

    public sealed class EditorJsonSerializer : Json.Serialization.IJsonSerializer<Editor>
    {
        public static readonly EditorJsonSerializer Instance = new EditorJsonSerializer();

        public Json.ImmutableJson Serialize(Editor value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            return value.EditorType switch
            {
                EditorType.Icon => EditorIconJsonSerializer.Instance.Serialize((EditorIcon)value),
                EditorType.Curve => EditorCurveJsonSerializer.Instance.Serialize((EditorCurve)value),
                EditorType.Plot => EditorPlotJsonSerializer.Instance.Serialize((EditorPlot)value),
                EditorType.InteractiveMap => EditorInteractiveMapJsonSerializer.Instance.Serialize((EditorInteractiveMap)value),
                EditorType.Breadcrumbs => EditorBreadcrumbsJsonSerializer.Instance.Serialize((EditorBreadcrumbs)value),
                _ => throw new System.ArgumentException("Invalid variant tag")
            };
        }

        public Editor Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            EditorType editorType = EditorTypeJsonSerializer.Instance.Deserialize(json["editor_type"]);
            return editorType switch
            {
                EditorType.Icon => EditorIconJsonSerializer.Instance.Deserialize(json),
                EditorType.Curve => EditorCurveJsonSerializer.Instance.Deserialize(json),
                EditorType.Plot => EditorPlotJsonSerializer.Instance.Deserialize(json),
                EditorType.InteractiveMap => EditorInteractiveMapJsonSerializer.Instance.Deserialize(json),
                EditorType.Breadcrumbs => EditorBreadcrumbsJsonSerializer.Instance.Deserialize(json),
                _ => throw new System.ArgumentException("Invalid variant tag")
            };
        }
    }

    public sealed class EditorIconJsonSerializer : Json.Serialization.IJsonSerializer<EditorIcon>
    {
        public static readonly EditorIconJsonSerializer Instance = new EditorIconJsonSerializer();

        public Json.ImmutableJson Serialize(EditorIcon value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));

            if (value.Atlas == null)
                throw new System.ArgumentException("Required property Atlas is null", nameof(value));

            return new Json.JsonObject
            {
                ["editor_type"] = EditorTypeJsonSerializer.Instance.Serialize(value.EditorType),
                ["scope"] = EditorScopeJsonSerializer.Instance.Serialize(value.Scope),
                ["atlas"] = JsonSerializer.String.Serialize(value.Atlas),
                ["icon_width"] = JsonSerializer.Int.Serialize(value.IconWidth),
                ["icon_height"] = JsonSerializer.Int.Serialize(value.IconHeight),
                ["default"] = JsonSerializer.Int.Serialize(value.Default)
            };
        }

        public EditorIcon Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var result = new EditorIcon();
            Deserialize(json, result);
            return result;
        }

        public void Deserialize(Json.ImmutableJson json, EditorIcon value)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (json.AsObject.TryGetValue("scope", out var jsonScope) && !jsonScope.IsNull)
                value.Scope = EditorScopeJsonSerializer.Instance.Deserialize(jsonScope);
            if (json.AsObject.TryGetValue("atlas", out var jsonAtlas) && !jsonAtlas.IsNull)
                value.Atlas = JsonSerializer.String.Deserialize(jsonAtlas);
            value.IconWidth = JsonSerializer.Int.Deserialize(json["icon_width"]);
            value.IconHeight = JsonSerializer.Int.Deserialize(json["icon_height"]);
            value.Default = JsonSerializer.Int.Deserialize(json["default"]);
        }
    }

    public sealed class EditorCurveKnotJsonSerializer : Json.Serialization.IJsonSerializer<EditorCurveKnot>
    {
        public static readonly EditorCurveKnotJsonSerializer Instance = new EditorCurveKnotJsonSerializer();

        public Json.ImmutableJson Serialize(EditorCurveKnot value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));

            return new Json.JsonObject
            {
                ["position"] = VectorJsonSerializer.Instance.Serialize(value.Position),
                ["tangent_in"] = JsonSerializer.Double.Serialize(value.TangentIn),
                ["tangent_out"] = JsonSerializer.Double.Serialize(value.TangentOut)
            };
        }

        public EditorCurveKnot Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var position = VectorJsonSerializer.Instance.Deserialize(json["position"]);
            var tangentIn = JsonSerializer.Double.Deserialize(json["tangent_in"]);
            var tangentOut = JsonSerializer.Double.Deserialize(json["tangent_out"]);
            return new EditorCurveKnot(position, tangentIn, tangentOut);
        }
    }

    public sealed class EditorCurveDataJsonSerializer : Json.Serialization.IJsonSerializer<EditorCurveData>
    {
        public static readonly EditorCurveDataJsonSerializer Instance = new EditorCurveDataJsonSerializer();

        public Json.ImmutableJson Serialize(EditorCurveData value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (value.Knots == null)
                throw new System.ArgumentException("Required property Knots is null", nameof(value));
            return new Json.JsonObject
            {
                ["knots"] = JsonSerializer.ReadOnlyList(EditorCurveKnotJsonSerializer.Instance).Serialize(value.Knots)
            };
        }

        public EditorCurveData Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            IReadOnlyList<EditorCurveKnot> knots;
            if (json.AsObject.TryGetValue("knots", out var jsonKnots) && !jsonKnots.IsNull)
                knots = JsonSerializer.ReadOnlyList(EditorCurveKnotJsonSerializer.Instance).Deserialize(jsonKnots);
            else
                knots = System.Array.Empty<EditorCurveKnot>();
            return new EditorCurveData(knots);
        }
    }

    public sealed class EditorCurvePresetJsonSerializer : Json.Serialization.IJsonSerializer<EditorCurvePreset>
    {
        public static readonly EditorCurvePresetJsonSerializer Instance = new EditorCurvePresetJsonSerializer();

        public Json.ImmutableJson Serialize(EditorCurvePreset value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (value.Name == null)
                throw new System.ArgumentException("Required property Name is null", nameof(value));
            if (value.CurveData == null)
                throw new System.ArgumentException("Required property CurveData is null", nameof(value));
            return new Json.JsonObject
            {
                ["name"] = JsonSerializer.String.Serialize(value.Name),
                ["curve_data"] = EditorCurveDataJsonSerializer.Instance.Serialize(value.CurveData)
            };
        }

        public EditorCurvePreset Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var name = JsonSerializer.String.Deserialize(json["name"]);
            var curveData = EditorCurveDataJsonSerializer.Instance.Deserialize(json["curve_data"]);
            return new EditorCurvePreset(name, curveData);
        }
    }

    public sealed class EditorCurveJsonSerializer : Json.Serialization.IJsonSerializer<EditorCurve>
    {
        public static readonly EditorCurveJsonSerializer Instance = new EditorCurveJsonSerializer();

        public Json.ImmutableJson Serialize(EditorCurve value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));

            if (value.Presets == null)
                throw new System.ArgumentException("Required property Presets is null", nameof(value));
            var json = new Json.JsonObject();
            json["editor_type"] = EditorTypeJsonSerializer.Instance.Serialize(value.EditorType);
            json["scope"] = EditorScopeJsonSerializer.Instance.Serialize(value.Scope);
            json["default_viewport"] = RectJsonSerializer.Instance.Serialize(value.DefaultViewport);
            json["auto_scale_preview_x"] = JsonSerializer.Bool.Serialize(value.AutoScalePreviewX);
            json["auto_scale_preview_y"] = JsonSerializer.Bool.Serialize(value.AutoScalePreviewY);
            json["auto_scale_editor"] = JsonSerializer.Bool.Serialize(value.AutoScaleEditor);
            json["preview_width"] = JsonSerializer.Float.Serialize(value.PreviewWidth);
            json["preview_height"] = JsonSerializer.Float.Serialize(value.PreviewHeight);
            if (value.AxisXLabel != null)
                json["axis_x_label"] = JsonSerializer.String.Serialize(value.AxisXLabel);
            if (value.AxisYLabel != null)
                json["axis_y_label"] = JsonSerializer.String.Serialize(value.AxisYLabel);
            json["presets"] = JsonSerializer.List(EditorCurvePresetJsonSerializer.Instance).Serialize(value.Presets);
            return json;
        }

        public EditorCurve Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var result = new EditorCurve();
            Deserialize(json, result);
            return result;
        }

        public void Deserialize(Json.ImmutableJson json, EditorCurve value)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (json.AsObject.TryGetValue("scope", out var jsonScope) && !jsonScope.IsNull)
                value.Scope = EditorScopeJsonSerializer.Instance.Deserialize(jsonScope);
            value.DefaultViewport = RectJsonSerializer.Instance.Deserialize(json["default_viewport"]);
            if (json.AsObject.TryGetValue("auto_scale_preview_x", out var jsonAutoScalePreviewX) && !jsonAutoScalePreviewX.IsNull)
                value.AutoScalePreviewX = JsonSerializer.Bool.Deserialize(jsonAutoScalePreviewX);
            if (json.AsObject.TryGetValue("auto_scale_preview_y", out var jsonAutoScalePreviewY) && !jsonAutoScalePreviewY.IsNull)
                value.AutoScalePreviewY = JsonSerializer.Bool.Deserialize(jsonAutoScalePreviewY);
            if (json.AsObject.TryGetValue("auto_scale_editor", out var jsonAutoScaleEditor) && !jsonAutoScaleEditor.IsNull)
                value.AutoScaleEditor = JsonSerializer.Bool.Deserialize(jsonAutoScaleEditor);
            if (json.AsObject.TryGetValue("preview_width", out var jsonPreviewWidth) && !jsonPreviewWidth.IsNull)
                value.PreviewWidth = JsonSerializer.Float.Deserialize(jsonPreviewWidth);
            if (json.AsObject.TryGetValue("preview_height", out var jsonPreviewHeight) && !jsonPreviewHeight.IsNull)
                value.PreviewHeight = JsonSerializer.Float.Deserialize(jsonPreviewHeight);
            if (json.AsObject.TryGetValue("axis_x_label", out var jsonAxisXLabel) && !jsonAxisXLabel.IsNull)
                value.AxisXLabel = JsonSerializer.String.Deserialize(jsonAxisXLabel);
            if (json.AsObject.TryGetValue("axis_y_label", out var jsonAxisYLabel) && !jsonAxisYLabel.IsNull)
                value.AxisYLabel = JsonSerializer.String.Deserialize(jsonAxisYLabel);
            if (json.AsObject.TryGetValue("presets", out var jsonPresets) && !jsonPresets.IsNull)
                value.Presets = JsonSerializer.List(EditorCurvePresetJsonSerializer.Instance).Deserialize(jsonPresets);
        }
    }

    public sealed class PlotTypeJsonSerializer : Json.Serialization.IJsonSerializer<PlotType>, Json.Serialization.IJsonKeySerializer<PlotType>
    {
        public static readonly PlotTypeJsonSerializer Instance = new PlotTypeJsonSerializer();

        public Json.ImmutableJson Serialize(PlotType value)
        {
            return SerializeKey(value);
        }

        public PlotType Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            return DeserializeKey(json.AsString);
        }

        public string SerializeKey(PlotType value)
        {
            return value switch
            {
                PlotType.Points => "points",
                _ => throw new System.ArgumentOutOfRangeException(nameof(value))
            };
        }

        public PlotType DeserializeKey(string jsonKey)
        {
            return jsonKey switch
            {
                "points" => PlotType.Points,
                _ => throw new System.ArgumentOutOfRangeException(nameof(jsonKey))
            };
        }
    }

    public sealed class EditorPlotDataJsonSerializer : Json.Serialization.IJsonSerializer<EditorPlotData>
    {
        public static readonly EditorPlotDataJsonSerializer Instance = new EditorPlotDataJsonSerializer();

        public Json.ImmutableJson Serialize(EditorPlotData value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (value.Points == null)
                throw new System.ArgumentException("Required property Points is null", nameof(value));
            return new Json.JsonObject
            {
                ["points"] = JsonSerializer.ReadOnlyList(VectorJsonSerializer.Instance).Serialize(value.Points)
            };
        }

        public EditorPlotData Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            IReadOnlyList<System.Windows.Point> points;
            if (json.AsObject.TryGetValue("points", out var jsonPoints) && !jsonPoints.IsNull)
                points = JsonSerializer.ReadOnlyList(VectorJsonSerializer.Instance).Deserialize(jsonPoints);
            else
                points = System.Array.Empty<System.Windows.Point>();
            return new EditorPlotData(points);
        }
    }

    public sealed class EditorPlotJsonSerializer : Json.Serialization.IJsonSerializer<EditorPlot>
    {
        public static readonly EditorPlotJsonSerializer Instance = new EditorPlotJsonSerializer();

        public Json.ImmutableJson Serialize(EditorPlot value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));

            return new Json.JsonObject
            {
                ["editor_type"] = EditorTypeJsonSerializer.Instance.Serialize(value.EditorType),
                ["scope"] = EditorScopeJsonSerializer.Instance.Serialize(value.Scope),
                ["default_viewport"] = RectJsonSerializer.Instance.Serialize(value.DefaultViewport),
                ["plot_type"] = PlotTypeJsonSerializer.Instance.Serialize(value.PlotType),
                ["auto_scale_preview_x"] = JsonSerializer.Bool.Serialize(value.AutoScalePreviewX),
                ["auto_scale_preview_y"] = JsonSerializer.Bool.Serialize(value.AutoScalePreviewY),
                ["auto_scale_editor"] = JsonSerializer.Bool.Serialize(value.AutoScaleEditor),
                ["preview_width"] = JsonSerializer.Float.Serialize(value.PreviewWidth),
                ["preview_height"] = JsonSerializer.Float.Serialize(value.PreviewHeight)
            };
        }

        public EditorPlot Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var result = new EditorPlot();
            Deserialize(json, result);
            return result;
        }

        public void Deserialize(Json.ImmutableJson json, EditorPlot value)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (json.AsObject.TryGetValue("scope", out var jsonScope) && !jsonScope.IsNull)
                value.Scope = EditorScopeJsonSerializer.Instance.Deserialize(jsonScope);
            value.DefaultViewport = RectJsonSerializer.Instance.Deserialize(json["default_viewport"]);
            value.PlotType = PlotTypeJsonSerializer.Instance.Deserialize(json["plot_type"]);
            if (json.AsObject.TryGetValue("auto_scale_preview_x", out var jsonAutoScalePreviewX) && !jsonAutoScalePreviewX.IsNull)
                value.AutoScalePreviewX = JsonSerializer.Bool.Deserialize(jsonAutoScalePreviewX);
            if (json.AsObject.TryGetValue("auto_scale_preview_y", out var jsonAutoScalePreviewY) && !jsonAutoScalePreviewY.IsNull)
                value.AutoScalePreviewY = JsonSerializer.Bool.Deserialize(jsonAutoScalePreviewY);
            if (json.AsObject.TryGetValue("auto_scale_editor", out var jsonAutoScaleEditor) && !jsonAutoScaleEditor.IsNull)
                value.AutoScaleEditor = JsonSerializer.Bool.Deserialize(jsonAutoScaleEditor);
            if (json.AsObject.TryGetValue("preview_width", out var jsonPreviewWidth) && !jsonPreviewWidth.IsNull)
                value.PreviewWidth = JsonSerializer.Float.Deserialize(jsonPreviewWidth);
            if (json.AsObject.TryGetValue("preview_height", out var jsonPreviewHeight) && !jsonPreviewHeight.IsNull)
                value.PreviewHeight = JsonSerializer.Float.Deserialize(jsonPreviewHeight);
        }
    }

    public sealed class EditorInteractiveMapJsonSerializer : Json.Serialization.IJsonSerializer<EditorInteractiveMap>
    {
        public static readonly EditorInteractiveMapJsonSerializer Instance = new EditorInteractiveMapJsonSerializer();

        public Json.ImmutableJson Serialize(EditorInteractiveMap value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));

            if (value.Title == null)
                throw new System.ArgumentException("Required property Title is null", nameof(value));
            return new Json.JsonObject
            {
                ["editor_type"] = EditorTypeJsonSerializer.Instance.Serialize(value.EditorType),
                ["scope"] = EditorScopeJsonSerializer.Instance.Serialize(value.Scope),
                ["title"] = JsonSerializer.String.Serialize(value.Title)
            };
        }

        public EditorInteractiveMap Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var result = new EditorInteractiveMap();
            Deserialize(json, result);
            return result;
        }

        public void Deserialize(Json.ImmutableJson json, EditorInteractiveMap value)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (json.AsObject.TryGetValue("scope", out var jsonScope) && !jsonScope.IsNull)
                value.Scope = EditorScopeJsonSerializer.Instance.Deserialize(jsonScope);
            if (json.AsObject.TryGetValue("title", out var jsonTitle) && !jsonTitle.IsNull)
                value.Title = JsonSerializer.String.Deserialize(jsonTitle);
        }
    }

    public sealed class BreadcrumbsDataJsonSerializer : Json.Serialization.IJsonSerializer<BreadcrumbsData>
    {
        public static readonly BreadcrumbsDataJsonSerializer Instance = new BreadcrumbsDataJsonSerializer();

        public Json.ImmutableJson Serialize(BreadcrumbsData value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (value.Name == null)
                throw new System.ArgumentException("Required property Name is null", nameof(value));

            var json = new Json.JsonObject();
            json["name"] = JsonSerializer.String.Serialize(value.Name);
            if (value.Items != null)
                json["items"] = JsonSerializer.ReadOnlyList(BreadcrumbsDataJsonSerializer.Instance).Serialize(value.Items);
            return json;
        }

        public BreadcrumbsData Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var name = JsonSerializer.String.Deserialize(json["name"]);
            IReadOnlyList<BreadcrumbsData>? items;
            if (json.AsObject.TryGetValue("items", out var jsonItems) && !jsonItems.IsNull)
                items = JsonSerializer.ReadOnlyList(BreadcrumbsDataJsonSerializer.Instance).Deserialize(jsonItems);
            else
                items = null;
            return new BreadcrumbsData(name, items);
        }
    }

    public sealed class EditorBreadcrumbsJsonSerializer : Json.Serialization.IJsonSerializer<EditorBreadcrumbs>
    {
        public static readonly EditorBreadcrumbsJsonSerializer Instance = new EditorBreadcrumbsJsonSerializer();

        public Json.ImmutableJson Serialize(EditorBreadcrumbs value)
        {
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));

            if (value.Items == null)
                throw new System.ArgumentException("Required property Items is null", nameof(value));
            return new Json.JsonObject
            {
                ["editor_type"] = EditorTypeJsonSerializer.Instance.Serialize(value.EditorType),
                ["scope"] = EditorScopeJsonSerializer.Instance.Serialize(value.Scope),
                ["items"] = JsonSerializer.ReadOnlyList(BreadcrumbsDataJsonSerializer.Instance).Serialize(value.Items)
            };
        }

        public EditorBreadcrumbs Deserialize(Json.ImmutableJson json)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            var result = new EditorBreadcrumbs();
            Deserialize(json, result);
            return result;
        }

        public void Deserialize(Json.ImmutableJson json, EditorBreadcrumbs value)
        {
            if (json == null)
                throw new System.ArgumentNullException(nameof(json));
            if (value == null)
                throw new System.ArgumentNullException(nameof(value));
            if (json.AsObject.TryGetValue("scope", out var jsonScope) && !jsonScope.IsNull)
                value.Scope = EditorScopeJsonSerializer.Instance.Deserialize(jsonScope);
            if (json.AsObject.TryGetValue("items", out var jsonItems) && !jsonItems.IsNull)
                value.Items = JsonSerializer.ReadOnlyList(BreadcrumbsDataJsonSerializer.Instance).Deserialize(jsonItems);
        }
    }
}