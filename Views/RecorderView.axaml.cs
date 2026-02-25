using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using OmniScribe.ViewModels;
using System.Linq;

namespace OmniScribe.Views;

public partial class RecorderView : UserControl
{
    public RecorderView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    var path = file.Path?.LocalPath;
                    if (path != null && DataContext is RecorderViewModel vm)
                    {
                        vm.HandleFileDrop(path);
                    }
                }
            }
        }
        e.Handled = true;
    }
}
