using Hercules.Controls;
using Hercules.Forms.Presentation;
using Hercules.Forms.Schema;
using Hercules.Shortcuts;
using Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Hercules.Forms.Elements
{
    public abstract class CollectionElement<T> : ExpanderElement<T>, IClearElement
        where T : Element, ICollectionItemElement
    {
        protected CollectionElement(IContainer parent, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, originalJson, transaction)
        {
            AddElementCommand = Commands.Execute(DoAddElement);
        }

        List<T>? childrenBeforeDrag;

        public ICommand AddElementCommand { get; }

        protected abstract T CreateEmpty(ITransaction transaction);

        void DoAddElement()
        {
            Form.Run(transaction =>
                {
                    var empty = CreateEmpty(transaction);
                    empty.ExpandInitially(transaction);
                    Add(empty, transaction);
                }, this);
        }

        public void Remove(T element, ITransaction transaction)
        {
            var index = element.Index;
            Children.Remove(element);

            void Undo(ITransaction t)
            {
                element.OnAdded(t);
                Children.Insert(index, element);
                RestoreIndices(index);
            }

            void Redo(ITransaction t)
            {
                element.OnRemoved(t);
                Children.Remove(element);
                RestoreIndices(index);
            }

            transaction.RequestFullInvalidation();
            transaction.RefreshPresentation();
            transaction.AddUndoRedo(Undo, Redo);
            ValueChanged(transaction);
            element.OnRemoved(transaction);
            RestoreIndices(index);
        }

        protected void RestoreIndices(int sinceIndex = 0)
        {
            for (int i = sinceIndex; i < Children.Count; i++)
                Children[i].Index = i;
        }

        public void Add(T element, ITransaction transaction)
        {
            Children.Add(element);
            int index = Children.Count - 1;

            void Undo(ITransaction t)
            {
                element.OnRemoved(t);
                Children.Remove(element);
                RestoreIndices(index);
            }

            void Redo(ITransaction t)
            {
                element.OnAdded(t);
                Children.Add(element);
                RestoreIndices(index);
            }

            transaction.AddUndoRedo(Undo, Redo);
            transaction.RefreshPresentation();
            transaction.RequestFullInvalidation();
            RestoreIndices(index);
            ValueChanged(transaction);
            element.OnAdded(transaction);
        }

        public void Insert(T element, int index, ITransaction transaction)
        {
            Children.Insert(index, element);

            void Undo(ITransaction t)
            {
                element.OnRemoved(t);
                Children.Remove(element);
                RestoreIndices(index);
            }

            void Redo(ITransaction t)
            {
                element.OnAdded(t);
                Children.Insert(index, element);
                RestoreIndices(index);
            }

            transaction.AddUndoRedo(Undo, Redo);
            transaction.RefreshPresentation();
            transaction.RequestFullInvalidation();
            ValueChanged(transaction);
            element.OnAdded(transaction);
            RestoreIndices(index);
        }

        public void PreviewMove(T from, T to)
        {
            Children.Move(Children.IndexOf(from), Children.IndexOf(to));
            Form.RefreshPresentation();
        }

        public void Move(T from, T to, ITransaction transaction)
        {
            var fromIndex = Children.IndexOf(from);
            var toIndex = Children.IndexOf(to);
            Children.Move(fromIndex, toIndex);
            RestoreIndices();
            var oldChildren = childrenBeforeDrag!.ToArray();
            var newChildren = Children.ToArray();

            void Undo(ITransaction t)
            {
                t.RequestFullInvalidation();
                t.RefreshPresentation();
                Children.Clear();
                Children.AddRange(oldChildren);
                RestoreIndices();
            }

            void Redo(ITransaction t)
            {
                t.RequestFullInvalidation();
                t.RefreshPresentation();
                Children.Clear();
                Children.AddRange(newChildren);
                RestoreIndices();
            }

            transaction.AddUndoRedo(Undo, Redo);
            transaction.RefreshPresentation();
            transaction.RequestFullInvalidation();
            ValueChanged(transaction);
        }

        public void DragStarted(T item)
        {
            childrenBeforeDrag = Children.ToList();
        }

        public void Clear()
        {
            Form.Run(transaction =>
                {
                    foreach (var child in Children)
                    {
                        child.OnRemoved(transaction);
                    }
                    var oldChildren = Children.ToArray();
                    Children.Clear();

                    void Undo(ITransaction t)
                    {
                        foreach (var child in oldChildren)
                        {
                            Children.Add(child);
                            child.OnAdded(t);
                        }
                    }

                    void Redo(ITransaction t)
                    {
                        foreach (var child in Children)
                        {
                            child.OnRemoved(t);
                        }
                        Children.Clear();
                    }

                    transaction.AddUndoRedo(Undo, Redo);
                    transaction.RefreshPresentation();
                    transaction.RequestFullInvalidation();
                    ValueChanged(transaction);
                }, this);
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);

            if (context.IsPropertyEditor)
            {
                context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("ExpanderElementPropertyToggle"), gridColumn: 0, width: 16));
                context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("ExpanderElementPropertyContent")));

                if (IsExpanded)
                {
                    context.Indent(16);
                    foreach (var child in GetChildren())
                    {
                        child.Present(context);
                    }

                    context.AddRow(proxy);
                    context.AddItem(proxy.Item2 ??= new VirtualRowItem(this, ControlPools.GetPool("CollectionPropertyAddElement")));
                    context.Outdent();
                }
            }
            else
            {
                if (IsExpanded)
                {
                    context.Indent(context.Left);
                    context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("ExpanderElement"), isTabStop: true));
                    foreach (var child in GetChildren())
                    {
                        child.Present(context);
                    }

                    context.AddRow(proxy);
                    context.AddItem(proxy.Item1 ??= new VirtualRowItem(this, ControlPools.GetPool("AddElement"), isTabStop: false), height: 22);
                    context.Outdent();
                }
                else
                {
                    context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("ExpanderElement"), isTabStop: true));
                }
            }
        }
    }

    public class ListItem : ProxyElement, IDropTargetElement, IDuplicateElement, ICollectionItemElement
    {
        public ListItem(ListElement parent, SchemaType type, ImmutableJson? json, ImmutableJson? originalJson, int index, int? originalIndex, ITransaction transaction)
            : base(parent, transaction)
        {
            this.Type = type;
            this.index = index;
            this.OriginalIndex = originalIndex;
            this.Element = Form.Factory.Create(this, type, json, originalJson, transaction);
            this.RemoveCommand = Commands.Execute(DoRemove);
        }

        public override SchemaType Type { get; }

        public ICommand RemoveCommand { get; }

        public ListElement List => (ListElement)Parent;

        private int index;

        public int Index
        {
            get => index;
            set => SetField(ref index, value);
        }

        public int? OriginalIndex { get; private set; }

        private void DoRemove()
        {
            Form.Run(transaction => List.Remove(this, transaction), this);
        }

        public bool AllowDrop(object data) => data switch
        {
            ListItem item => item.Parent == Parent,
            _ => false,
        };

        public void DragEnter(object data)
        {
            var item = (ListItem)data;
            List.PreviewMove(item, this);
        }

        public void DragLeave(object data)
        {
            // var item = element as ListItem;
            // Parent.PreviewMove(item, this);
        }

        public void Drop(object data)
        {
            var item = (ListItem)data;
            Form.Run(transaction => List.Move(item, this, transaction), this);
        }

        bool isDragged;

        public bool IsDragged
        {
            get => isDragged;
            set
            {
                if (isDragged != value)
                {
                    isDragged = value;
                    if (isDragged)
                        List.DragStarted(this);
                    RaisePropertyChanged();
                }
            }
        }

        public override JsonPath Path => Parent.Path.AppendArray(Index);

        public virtual void Duplicate()
        {
            Form.Run(transaction =>
            {
                var item = Form.Factory.CreateListItem(List, Type, Json, null, Index + 1, null, transaction);
                item.ExpandInitially(transaction);
                List.Insert(item, Index + 1, transaction);
            }, this);
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            context.AddRow(proxy);
            var dock = context.IsPropertyEditor ? HorizontalDock.Right : HorizontalDock.Left;
            var pool = context.IsPropertyEditor ? "ListItemPropertyDelete" : "ListItem";
            var width = context.IsPropertyEditor ? 16 : 54;
            context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool(pool), dock: dock, width: width));
            Element.Present(context);
        }

        public void OnRemoved(ITransaction transaction)
        {
            SetIsActive(false, transaction);
        }

        public void OnAdded(ITransaction transaction)
        {
            SetIsActive(true, transaction);
        }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            base.Validate(invalidateChildren, transaction, out isValid, out isModified);
            isModified = isModified || Index != OriginalIndex;
        }

        public void Rebase(ImmutableJson? newOriginalJson, int? newOriginalIndex, ITransaction transaction)
        {
            SetOriginalJson(newOriginalJson, transaction);
            OriginalIndex = newOriginalIndex;
        }
    }

    public class ListElement : CollectionElement<ListItem>, IPasteChildElement, IDropTargetElement, ISortableElement
    {
        public ListElement(IContainer parent, ListSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, originalJson, transaction)
        {
            ListType = type;
            if (json != null && json.IsArray)
            {
                var jsonArray = json.AsArray;
                for (int i = 0; i < jsonArray.Count; i++)
                {
                    var child = jsonArray[i];
                    var originalChild = SafeJson.GetItem(originalJson, i);
                    Children.Add(Form.Factory.CreateListItem(this, type.ItemType, child, originalChild, i, originalChild == null ? null : (int?)i, transaction));
                }
            }

            Children.CollectionChanged += (sender, e) => Caption = GetCaption();
            Caption = GetCaption();
        }

        public ListSchemaType ListType { get; }

        public override SchemaType Type => ListType;

        string GetCaption()
        {
            return $"[{Children.Count} items]";
        }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            base.Validate(invalidateChildren, transaction, out isValid, out isModified);
            if (!isModified && OriginalJson == null)
            {
                isModified = Children.Count > 0 || !ImmutableJson.Equals(ListType.Default, ImmutableJson.EmptyArray);
            }
            if (!isModified && OriginalJson != null)
            {
                isModified = !OriginalJson.IsArray || OriginalJson.AsArray.Count != Children.Count;
            }
        }

        protected override ListItem CreateEmpty(ITransaction transaction)
        {
            return Form.Factory.CreateListItem(this, ListType.ItemType, null, null, -1, null, transaction);
        }

        public override ImmutableJson Json
        {
            get { return new JsonArray(Children.Select(v => v.Json)); }
        }

        public override Element GetByPath(JsonPath path)
        {
            var head = path.Head;
            Element result = this;
            if (head is JsonArrayPathNode arrayPathNode)
            {
                var index = arrayPathNode.Index;
                if (index >= 0 && index < this.Children.Count)
                    result = this.Children[index].GetByPath(path.Tail);
            }
            return result;
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            json ??= ListType.Default;
            if (json != null && json.IsArray)
            {
                int oldCount = Children.Count;
                int newCount = json.AsArray.Count;
                int commonCount = Math.Min(oldCount, newCount);
                for (int i = 0; i < commonCount; i++)
                {
                    Children[i].SetJson(json.AsArray[i], transaction);
                }
                if (oldCount > newCount)
                {
                    var toRemove = Children.Skip(commonCount).ToList();
                    foreach (var item in toRemove)
                    {
                        Remove(item, transaction);
                    }
                }
                else if (newCount > oldCount)
                {
                    var toAdd = json.AsArray.Skip(commonCount).ToList();
                    foreach (var child in toAdd)
                    {
                        var originalIndex = Children.Count;
                        var original = SafeJson.GetItem(OriginalJson, originalIndex);
                        var item = Form.Factory.CreateListItem(this, ListType.ItemType, child, original, -1, original == null ? null : originalIndex, transaction);
                        Add(item, transaction);
                    }
                }
            }
            else
                transaction.AddWarning(Path, "Invalid value");
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            base.SetOriginalJson(newOriginalJson, transaction);
            foreach (var item in Children)
            {
                var originalChild = SafeJson.GetItem(newOriginalJson, item.Index);
                item.Rebase(originalChild, originalChild == null ? (int?)null : item.Index, transaction);
            }
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            // TODO: be smarter than that
            var result = false;
            foreach (var item in Children)
            {
                if (item.Inherit(SafeJson.GetItem(baseJson, item.Index)))
                    result = true;
            }
            var baseCount = baseJson != null && baseJson.IsArray ? baseJson.AsArray.Count : -1;
            return result || Children.Count != baseCount;
        }

        public Element? PasteElement(ImmutableJson json, ITransaction transaction)
        {
            var item = Form.Factory.CreateListItem(this, ListType.ItemType, json, null, -1, null, transaction);
            item.ExpandInitially(transaction);
            Add(item, transaction);
            return item;
        }

        private IEnumerable<IShortcut> GetShortcutsFromDropData(object data)
        {
            if (data is IEnumerable list)
                return list.OfType<IShortcutProvider>().Select(sp => sp.Shortcut);
            else if (data is IShortcutProvider sp)
            {
                var shortcut = sp.Shortcut;
                var handler = ListType.ShortcutService.GetHandler(shortcut);
                if (handler.IsFolder)
                    return GetShortcutsFromDropData(handler.GetItems(shortcut)!);
                else
                    return shortcut.Yield();
            }
            else
                return Enumerable.Empty<IShortcut>();
        }

        public bool AllowDrop(object data)
        {
            return GetShortcutsFromDropData(data).Any(shortcut => ListType.ItemType.TranslateShortcut(shortcut) != null);
        }

        public void DragEnter(object data)
        {
        }

        public void DragLeave(object data)
        {
        }

        public void Drop(object data)
        {
            Form.Run(transaction =>
            {
                foreach (var shortcut in GetShortcutsFromDropData(data))
                {
                    var json = ListType.ItemType.TranslateShortcut(shortcut);
                    if (json != null)
                    {
                        var item = Form.Factory.CreateListItem(this, ListType.ItemType, json, null, -1, null, transaction);
                        item.ExpandInitially(transaction);
                        Add(item, transaction);
                    }
                }
            }, this);
        }

        public void Sort()
        {
            Form.Run(transaction =>
            {
                var oldChildren = Children.ToArray();

                Children.Clear();
                Children.AddRange(oldChildren.OrderBy(item => item.DeepElement));

                void Undo(ITransaction t)
                {
                    Children.Clear();
                    Children.AddRange(oldChildren);
                    RestoreIndices(0);
                }

                void Redo(ITransaction t)
                {
                    Children.Clear();
                    Children.AddRange(oldChildren.OrderBy(item => item.DeepElement));
                    RestoreIndices(0);
                }

                RestoreIndices(0);
                transaction.AddUndoRedo(Undo, Redo);
                transaction.RefreshPresentation();
                transaction.RequestFullInvalidation();
                ValueChanged(transaction);
            }, this);
        }

        public bool CanSort => ListType.ItemType.IsComparable;
    }

    public class Pair : Container, IDuplicateElement, ICollectionItemElement
    {
        public SchemaType KeyType => Dict.DictType.KeyType;

        public SchemaType ValueTypePerKey(string key) => Dict.DictType.ValueTypePerKey(key);

        public override SchemaType Type => ValueElement.Type;

        public DictElement Dict => (DictElement)Parent;

        public Element KeyElement { get; }
        public Element ValueElement { get; private set; }

        public Pair(DictElement parent, string jsonKey, ImmutableJson? jsonValue, string? originalJsonKey, ImmutableJson? originalJsonValue, int index, ITransaction transaction)
            : base(parent, transaction)
        {
            Index = index;
            this.KeyElement = Form.Factory.CreateKey(this, KeyType, jsonKey, originalJsonKey, transaction);
            this.ValueElement = Form.Factory.Create(this, ValueTypePerKey(jsonKey), jsonValue, originalJsonValue, transaction);
            this.RemoveCommand = Commands.Execute(DoRemove);
        }

        public int Index { get; set; }

        public override IEnumerable<Element> GetChildren()
        {
            yield return KeyElement;
            yield return ValueElement;
        }

        public ICommand RemoveCommand { get; }

        public void DoRemove()
        {
            Form.Run(transaction => Dict.Remove(this, transaction), this);
        }

        public override ImmutableJson Json => new JsonObject { { KeyElement.JsonKey, ValueElement.Json } };

        public override ImmutableJson? OriginalJson => ValueElement.OriginalJson;

        public override JsonPath Path => Parent.Path.AppendObject(KeyElement.JsonKey);

        public override JsonPath GetChildPath(Element child)
        {
            if (child == KeyElement)
                return Parent.Path.AppendObjectKey(KeyElement.JsonKey);
            else
                return ValuePath;
        }

        public override bool IsJsonKeyChild(Element child)
        {
            return child == KeyElement;
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            if (json != null && json.IsObject && json.AsObject.Count == 1)
            {
                var pair = json.AsObject.First();
                KeyElement.SetJsonKey(pair.Key, transaction);
                ValueElement.SetJson(pair.Value, transaction);
            }
            else
                transaction.AddWarning(Path, "Invalid pair");
        }

        public void Duplicate()
        {
            Form.Run(transaction =>
            {
                var item = new Pair(Dict, KeyElement.JsonKey, ValueElement.Json, null, null, Index + 1, transaction);
                item.ExpandInitially(transaction);
                Dict.Insert(item, Index + 1, transaction);
            }, this);
        }

        public override void Present(PresentationContext context)
        {
            var proxy = context.GetProxy(this);
            if (context.IsPropertyEditor)
            {
                context.AddRow(proxy, 0);
                context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("ListItemPropertyDelete"), dock: HorizontalDock.Right, width: 16));
                context.GridColumn = 1;
                KeyElement.Present(context);
                context.GridColumn = null;
                ValueElement.Present(context);
            }
            else
            {
                if (Dict.DictType.Compact)
                {
                    context.AddRow(proxy, 0);
                    context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("Pair"), width: 20));
                    KeyElement.Present(context);
                    context.AddMargin(4);
                    ValueElement.Present(context);
                }
                else
                {
                    context.AddRow(proxy, 0);
                    context.AddItem(proxy.Item0 ??= new VirtualRowItem(this, ControlPools.GetPool("Pair"), width: 20));
                    context.Indent(context.Left);
                    KeyElement.Present(context);
                    context.AddRow(proxy, 1);
                    ValueElement.Present(context);
                    context.Outdent();
                }
            }
        }

        public override void ChildChanged(ITransaction transaction)
        {
            var valueType = ValueTypePerKey(KeyElement.JsonKey);
            if (!ReferenceEquals(ValueElement.Type, valueType))
            {
                this.ValueElement = Form.Factory.Create(this, valueType, ValueElement.Json, ValueElement.OriginalJson, transaction);
                this.Invalidate(true, transaction);
                transaction.RefreshPresentation();
            }
            base.ChildChanged(transaction);
        }

        public void OnRemoved(ITransaction transaction)
        {
            SetIsActive(false, transaction);
        }

        public void OnAdded(ITransaction transaction)
        {
            SetIsActive(true, transaction);
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            throw new NotSupportedException();
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            return ValueElement.Inherit(baseJson);
        }

        public void Rebase(string? newOriginalKey, ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            var newOriginalJsonKey = newOriginalKey == null ? null : ImmutableJson.Create(newOriginalKey);
            KeyElement.SetOriginalJson(newOriginalJsonKey, transaction);
            ValueElement.SetOriginalJson(newOriginalJson, transaction);
        }
    }

    public class DictElement : CollectionElement<Pair>, IPasteChildElement, ISortableElement
    {
        public DictElement(IContainer parent, DictSchemaType type, ImmutableJson? json, ImmutableJson? originalJson, ITransaction transaction)
            : base(parent, originalJson, transaction)
        {
            DictType = type;
            if (json != null && json.IsObject)
            {
                var originalJsonObject = originalJson != null && originalJson.IsObject ? originalJson.AsObject : null;
                int i = 0;
                foreach (var pair in json.AsObject)
                {
                    ImmutableJson? originalValue = ReferenceEquals(json, originalJson) ? pair.Value : SafeJson.GetField(originalJson, pair.Key);
                    Children.Add(new Pair(this, pair.Key, pair.Value, originalValue == null ? null : pair.Key, originalValue, i, transaction));
                    i++;
                }
            }
            Children.CollectionChanged += (sender, e) => Caption = GetCaption();
            Caption = GetCaption();
        }

        public DictSchemaType DictType { get; }

        public override SchemaType Type => DictType;

        string GetCaption()
        {
            return $"[{Children.Count} pairs]";
        }

        protected override Pair CreateEmpty(ITransaction transaction)
        {
            return new Pair(this, string.Empty, null, null, null, -1, transaction);
        }

        public override ImmutableJson Json
        {
            get
            {
                var json = new JsonObject();
                foreach (var child in Children)
                {
                    var key = child.KeyElement.JsonKey;
                    if (!string.IsNullOrEmpty(key))
                        json[key] = child.ValueElement.Json;
                }
                return json;
            }
        }

        public override Element GetByPath(JsonPath path)
        {
            var head = path.Head;
            Element? result = this;
            if (head is JsonObjectKeyPathNode objectKeyPathNode)
            {
                var key = objectKeyPathNode.Key;
                var pair = this.Children.FirstOrDefault(p => p.KeyElement.JsonKey == key);
                if (pair != null)
                    result = pair.KeyElement.GetByPath(path.Tail);
            }
            else if (head is JsonObjectPathNode objectPathNode)
            {
                var key = objectPathNode.Key;
                var pair = this.Children.FirstOrDefault(p => p.KeyElement.JsonKey == key);
                if (pair != null)
                    result = pair.ValueElement.GetByPath(path.Tail);
            }
            return result;
        }

        public override void SetOriginalJson(ImmutableJson? newOriginalJson, ITransaction transaction)
        {
            base.SetOriginalJson(newOriginalJson, transaction);
            foreach (var pair in Children)
            {
                var key = pair.KeyElement.JsonKey;
                var originalValue = SafeJson.GetField(newOriginalJson, key);
                pair.Rebase(originalValue == null ? null : key, originalValue, transaction);
            }
        }

        protected override bool IsOverriden(ImmutableJson? baseJson)
        {
            var result = false;
            foreach (var pair in Children)
            {
                if (pair.Inherit(SafeJson.GetField(baseJson, pair.KeyElement.JsonKey)))
                    result = true;
            }
            var baseCount = baseJson != null && baseJson.IsObject ? baseJson.AsObject.Count : -1;
            return result || Children.Count != baseCount;
        }

        public override void SetJson(ImmutableJson? json, ITransaction transaction)
        {
            json ??= DictType.Default;
            if (json != null && json.IsObject)
            {
                int oldCount = Children.Count;
                int newCount = json.AsObject.Count;
                var obj = json.AsObject.ToList();
                int commonCount = Math.Min(oldCount, newCount);
                for (int i = 0; i < commonCount; i++)
                {
                    var pair = obj[i];
                    Children[i].SetJson(new JsonObject { { pair.Key, pair.Value } }, transaction);
                }
                if (oldCount > newCount)
                {
                    var toRemove = Children.Skip(commonCount).ToList();
                    foreach (var item in toRemove)
                    {
                        Remove(item, transaction);
                    }
                }
                else if (newCount > oldCount)
                {
                    var toAdd = obj.Skip(commonCount).ToList();
                    foreach (var child in toAdd)
                    {
                        var item = new Pair(this, child.Key, child.Value, null, null, -1, transaction);
                        Add(item, transaction);
                    }
                }
            }
            else
                transaction.AddWarning(Path, "Invalid value");
        }

        public Element? PasteElement(ImmutableJson json, ITransaction transaction)
        {
            if (json.IsObject && json.AsObject.Count == 1)
            {
                var pair = json.AsObject.First();
                var item = new Pair(this, pair.Key, pair.Value, null, null, -1, transaction);
                item.ExpandInitially(transaction);
                Add(item, transaction);
                return item;
            }
            else
            {
                transaction.AddWarning(Path, "Invalid value");
                return null;
            }
        }

        protected override void Validate(bool invalidateChildren, ITransaction transaction, out bool isValid, out bool isModified)
        {
            base.Validate(invalidateChildren, transaction, out isValid, out isModified);
            isValid = isValid && Children.Select(p => p.KeyElement.JsonKey).Distinct().Count() == Children.Count;
            if (!isModified && OriginalJson == null)
            {
                isModified = Children.Count > 0 || !ImmutableJson.Equals(DictType.Default, ImmutableJson.EmptyObject);
            }
            if (!isModified && OriginalJson != null)
            {
                isModified = !OriginalJson.IsObject || OriginalJson.AsObject.Count != Children.Count;
            }
        }

        public override void CollectIssues(IList<FormIssue> issues)
        {
            if (Children.Select(p => p.KeyElement.JsonKey).Distinct().Count() != Children.Count)
            {
                issues.Add(new FormIssue(FormIssueSeverity.Error, Path, "Dictionary contains duplicate keys"));
            }
        }

        public void Sort()
        {
            Form.Run(transaction =>
            {
                var oldChildren = Children.ToArray();

                Children.Clear();
                Children.AddRange(oldChildren.OrderBy(item => item.KeyElement));

                void Undo(ITransaction t)
                {
                    Children.Clear();
                    Children.AddRange(oldChildren);
                    RestoreIndices(0);
                }

                void Redo(ITransaction t)
                {
                    Children.Clear();
                    Children.AddRange(oldChildren.OrderBy(item => item.KeyElement));
                    RestoreIndices(0);
                }

                RestoreIndices(0);
                transaction.AddUndoRedo(Undo, Redo);
                transaction.RefreshPresentation();
                transaction.RequestFullInvalidation();
                ValueChanged(transaction);
            }, this);
        }

        public bool CanSort => DictType.KeyType.IsComparable;
    }
}