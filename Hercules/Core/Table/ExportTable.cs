using System.Collections.Generic;

namespace Hercules
{
    public interface IExportColumn
    {
        string Name { get; }
    }

    public interface IExportTable
    {
        IReadOnlyList<IExportColumn> ExportColumns { get; }
        int RowCount { get; }
        object? GetExportValue(int rowIndex, IExportColumn column);
    }

    public interface ITableExporter
    {
        void Export(IExportTable table, string fileName);
    }
}
