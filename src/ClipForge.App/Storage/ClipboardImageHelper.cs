using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClipForge.App.Storage;

/// <summary>
/// Builds a rich <see cref="DataObject"/> for image clips and copies it to the
/// clipboard reliably. Populating multiple formats (file drop + PNG stream +
/// bitmap) is what makes the image pasteable across apps — WPF's plain
/// <c>Clipboard.SetImage</c> writes only a DIB that many apps fail to paste.
/// The same object is reused for drag-and-drop out of the app.
/// </summary>
public static class ClipboardImageHelper
{
    /// <summary>
    /// Build a data object exposing the given image files as: a file drop list
    /// (Explorer, Office, mail), a PNG stream (browsers, chat), and a bitmap
    /// (Paint and classic image consumers). Missing files are skipped.
    /// </summary>
    public static DataObject BuildImageData(IReadOnlyList<string> paths)
    {
        var data = new DataObject();
        var existing = paths.Where(File.Exists).ToList();
        if (existing.Count == 0) return data;

        var files = new StringCollection();
        foreach (var p in existing) files.Add(p);
        data.SetFileDropList(files);

        // Image/PNG formats describe a single bitmap; use the first file.
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(existing[0]);
            bmp.EndInit();
            bmp.Freeze();
            data.SetImage(bmp);
            data.SetData("PNG", new MemoryStream(File.ReadAllBytes(existing[0])));
        }
        catch
        {
            // Unreadable/locked file: the file-drop list above is still valid.
        }
        return data;
    }

    /// <summary>Place a data object on the clipboard, retrying while it's locked.</summary>
    public static bool TrySetClipboard(DataObject data)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                System.Windows.Clipboard.SetDataObject(data, copy: true);   // flush so it survives app exit
                return true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                System.Threading.Thread.Sleep(20);
            }
        }
        return false;
    }
}
