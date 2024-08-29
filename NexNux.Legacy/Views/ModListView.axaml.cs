using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NexNux.Legacy.Models;
using NexNux.Legacy.ViewModels;
using ReactiveUI;

namespace NexNux.Legacy.Views;

public partial class ModListView : ReactiveUserControl<ModListViewModel>
{
    private Point? _dragStartPoint;
    private bool _isDragging;

    public ModListView()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.ShowModInstallDialog.RegisterHandler(DoShowModInstallDialogAsync)));
        this.WhenActivated(d => d(ViewModel!.ShowModUninstallDialog.RegisterHandler(DoShowModUninstallDialogAsync)));
        this.WhenActivated(d => d(ViewModel!.ShowErrorDialog.RegisterHandler(DoShowErrorDialogAsync)));
        this.WhenActivated(d => d(ViewModel!.ShowModExistsDialog.RegisterHandler(DoShowModExistsDialogAsync)));
        SetupGridHandlers();
        _isDragging = false;
    }

    private void SetupGridHandlers()
    {
        this.GetControl<DataGrid>("GridMods").AddHandler(DragDrop.DropEvent, DataGrid_Drop);
        this.GetControl<DataGrid>("GridMods").AddHandler(PointerMovedEvent, DataGrid_PointerMoved);
        this.GetControl<DataGrid>("GridMods").AddHandler(PointerReleasedEvent, DataGrid_PointerReleased);
        this.GetControl<DataGrid>("GridMods").CellPointerPressed += DataGridCell_PointerPressed;
        this.GetControl<DataGrid>("GridMods").AddHandler(DragDrop.DragOverEvent, DataGrid_DragOver);
        this.GetControl<DataGrid>("GridMods").AddHandler(PointerEnteredEvent, DataGrid_PointerEnter);
    }

    private void DataGrid_PointerEnter(object? sender, PointerEventArgs e)
    {
        // Workaround for the 'DragLeave' event not being fired, so the drop indicator line is only removed
        // after moving the mouse back into the datagrid, after dropping outside of it
        // This is broken on Linux
        ClearDropPoint();
        _dragStartPoint = null;
        _isDragging = false;
    }

    private void DataGrid_DragOver(object? sender, DragEventArgs e)
    {
        ClearDropPoint();
        var targetRow = ((Control)e.Source!).GetSelfAndVisualAncestors()
            .OfType<DataGridRow>()
            .FirstOrDefault();
        ShowDropPoint(e.Data.Get("DragSource") as DataGridRow, targetRow);
    }

    private void DataGridCell_PointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        // It is split into this, so that the dragging doesn't start as soon as one presses a row
        if (!e.PointerPressedEventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragStartPoint = e.PointerPressedEventArgs.GetPosition(null);
    }

    private void DataGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragStartPoint = null;
        _isDragging = false;
        ClearDropPoint();
    }

    private void DataGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStartPoint == null) return;
        if (_isDragging) return;
        if (sender is DataGrid)
        {
            var mousePosition = e.GetPosition(null);
            Vector positionDiff = _dragStartPoint.Value - mousePosition;

            var draggedEnough = Math.Abs(positionDiff.X) > 4 || Math.Abs(positionDiff.Y) > 4;

            if (draggedEnough && !_isDragging)
            {
                // Get the dragged row
                var sourceRow = ((Control)e.Source!).GetSelfAndVisualAncestors()
                    .OfType<DataGridRow>()
                    .FirstOrDefault();
                if (sourceRow == null) return;

                // Hacky workaround for getting the data, could be done via
                var dropData = this.GetControl<DataGrid>("GridMods").SelectedItem as Mod;
                if (dropData == null) return;

                // Actually do the interactivity part
                var dataObject = new DataObject();
                dataObject.Set("DraggedMod", dropData);
                dataObject.Set("SourceIndex", sourceRow.GetIndex());
                dataObject.Set("DragSource", sourceRow);

                DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
                _isDragging = true;
            }
        }
    }

    private void DataGrid_Drop(object? sender, DragEventArgs e)
    {
        if (sender is DataGrid && DataContext is ModListViewModel mlvm)
        {
            _dragStartPoint = null;
            _isDragging = false;

            // Retrieve we put into the DataModel
            var draggedMod = e.Data.Get("DraggedMod") as Mod;
            if (draggedMod == null) return;

            // Get targetRow by the drop position
            var targetRow = ((Control)e.Source!).GetSelfAndVisualAncestors()
                .OfType<DataGridRow>()
                .FirstOrDefault();

            var sourceIndex = (int)(e.Data.Get("SourceIndex") ?? throw new InvalidOperationException());
            var targetIndex =
                mlvm.VisibleMods.Count -
                1; // If dragged to the empty part of the DataGrid, it should just add it underneath

            // The actual drop/movement operation - this should be converted to command/interaction for MVVM
            if (targetRow != null) targetIndex = targetRow.GetIndex();

            // Indexes used to move items in the VM
            if (sourceIndex == targetIndex) return;
            mlvm.VisibleMods.Move(sourceIndex, targetIndex);

            // This is necessary for some reason, maybe because DataGrid cells are recycled?
            this.GetControl<DataGrid>("GridMods").ItemsSource = null;
            this.GetControl<DataGrid>("GridMods").ItemsSource = mlvm.VisibleMods;

            // Remove the line that indicates drop point
            ClearDropPoint();
        }
    }

    private async Task DoShowErrorDialogAsync(InteractionContext<string, bool> interactionContext)
    {
        var messageBox =
            MessageBoxManager.GetMessageBoxStandard("Error!", interactionContext.Input, ButtonEnum.Ok, Icon.Warning);
        await messageBox.ShowAsPopupAsync(this);
        interactionContext.SetOutput(true);
    }

    private async Task DoShowModInstallDialogAsync(InteractionContext<ModConfigViewModel, Mod?> interactionContext)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider == null) return;

        var fileTypeFilter = new List<FilePickerFileType>
        {
            new("Compressed Archives") { Patterns = new[] { "*.zip", "*.rar", "*.7z", "*.gzip" } }
        };

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Mod Archive",
            AllowMultiple = false,
            SuggestedStartLocation = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads),
            FileTypeFilter = fileTypeFilter
        });

        if (files.Count >= 1)
        {
            var filePath = files[0].TryGetLocalPath() ?? string.Empty;

            // Show a ModConfigView as a Dialog window to the current window
            var modConfigView = new ModConfigView();
            interactionContext.Input.UpdateModArchive(string.Join("", filePath));
            modConfigView.DataContext = interactionContext.Input;
            var mod = await modConfigView.ShowDialog<Mod>(GetMainWindow() ?? throw new InvalidOperationException());

            interactionContext.SetOutput(mod);
        }
        else
        {
            interactionContext.SetOutput(null);
        }
    }

    private async Task DoShowModUninstallDialogAsync(InteractionContext<Mod, bool> interactionContext)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(
            $"Uninstalling {interactionContext.Input}, are you sure?",
            $"This will also delete the files for \"{interactionContext.Input}\" from your system.", // currently this is a lie
            ButtonEnum.OkCancel,
            Icon.Warning
        );
        var result = await messageBox.ShowAsPopupAsync(GetMainWindow());
        interactionContext.SetOutput(result == ButtonResult.Ok);
    }

    private async Task DoShowModExistsDialogAsync(InteractionContext<Mod?, bool> interactionContext)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard(
            "Mod already exists",
            $"Mod \"{interactionContext.Input}\" already exists, continuing will merge the two while overriding existing files.", // currently this is a lie
            ButtonEnum.OkCancel,
            Icon.Info
        );
        var result = await messageBox.ShowAsPopupAsync(GetMainWindow());
        interactionContext.SetOutput(result == ButtonResult.Ok);
    }

    private void ShowDropPoint(DataGridRow? sourceRow, DataGridRow? targetRow)
    {
        if (sourceRow == null || targetRow == null) return;

        Point startPoint;
        Point endPoint;

        // When moving row 'down', it moves them to the index after the target
        if (targetRow.GetIndex() > sourceRow.GetIndex())
        {
            var startPointY = targetRow.Bounds.BottomLeft.Y + targetRow.Bounds.Height;
            var endPointY = targetRow.Bounds.BottomRight.Y + targetRow.Bounds.Height;

            startPoint = new Point(targetRow.Bounds.BottomLeft.X, startPointY);
            endPoint = new Point(targetRow.Bounds.BottomRight.X, endPointY);
        }
        // When moving rows 'up', its moves them to the index before the target
        else
        {
            var startPointY = targetRow.Bounds.TopLeft.Y + targetRow.Bounds.Height;
            var endPointY = targetRow.Bounds.TopRight.Y + targetRow.Bounds.Height;

            startPoint = new Point(targetRow.Bounds.TopLeft.X, startPointY);
            endPoint = new Point(targetRow.Bounds.TopRight.X, endPointY);
        }

        var line = new Line
        {
            Tag = "tempLine",
            StartPoint = startPoint,
            EndPoint = endPoint,
            Stroke = Brushes.Gray,
            StrokeThickness = 1,
            IsEnabled = false // So we can't drop on the actual indicator
        };
        this.GetControl<Canvas>("GridModsCanvas").Children.Add(line);
    }

    private void ClearDropPoint()
    {
        if (this.GetControl<Canvas>("GridModsCanvas").Children.Count <= 1) return;

        var existingLine = this.GetControl<Canvas>("GridModsCanvas").Children.OfType<Line>().FirstOrDefault();
        if (existingLine == null) return;
        this.GetControl<Canvas>("GridModsCanvas").Children.Remove(existingLine);
    }

    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            return lifetime.MainWindow;

        return null;
    }
}