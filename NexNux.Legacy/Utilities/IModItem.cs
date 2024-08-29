using System.Collections.ObjectModel;

namespace NexNux.Legacy.Utilities;

public interface IModItem
{
    public ObservableCollection<IModItem> SubItems { get; set; }
    public string ItemPath { get; }
    public string ItemName { get; set; }
}