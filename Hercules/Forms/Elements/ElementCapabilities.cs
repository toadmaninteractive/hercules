using Json;

namespace Hercules.Forms.Elements
{
    public interface ICollectionItemElement
    {
        int Index { get; set; }
        void OnRemoved(ITransaction transaction);
        void OnAdded(ITransaction transaction);
    }

    public interface IExpandableElement
    {
        bool IsExpanded { get; set; }
        void Expand(bool isExpanded, ITransaction transaction);
    }

    public interface IDuplicateElement
    {
        void Duplicate();
    }

    public interface IClearElement
    {
        void Clear();
    }

    public interface IPasteChildElement
    {
        Element? PasteElement(ImmutableJson json, ITransaction transaction);
    }

    public interface IDropTargetElement
    {
        bool AllowDrop(object data);

        void DragEnter(object data);

        void DragLeave(object data);

        void Drop(object data);
    }

    public interface ISortableElement
    {
        void Sort();
        bool CanSort { get; }
    }

    public interface IAutoCompleteElement
    {
        string? Value { get; }
        object Items { get; }
        void Submit(object? suggestion);
        bool Filter(object? item);
    }
}