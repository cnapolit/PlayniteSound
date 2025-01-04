using System;
using System.Windows;
using PlayniteSounds.Views.Models;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using PlayniteSounds.Models;
using static PlayniteSounds.Views.Models.DownloadPromptModel;

namespace PlayniteSounds.Views.Layouts
{
    public partial class DownloadPrompt
    {
        public DownloadPrompt() => InitializeComponent();

        private void PreviewSlider_DragStarted(object sender, DragStartedEventArgs e)
            => (DataContext as DownloadPromptModel).PausePreview();

        private void PreviewSlider_DragCompleted(object sender, DragCompletedEventArgs e)
            => (DataContext as DownloadPromptModel).PlayPreview();

        private void WatermarkSongTextBox_KeyDown(object sender, KeyEventArgs e)
            => ActOnEnter(sender, e, (DataContext as DownloadPromptModel).Sort);

        private void WatermarkAlbumTextBox_KeyDown(object sender, KeyEventArgs e)
            => ActOnEnter(sender, e, (DataContext as DownloadPromptModel).SearchOnEnter);

        private static void ActOnEnter(object sender, KeyEventArgs e, Action<string> action)
        {
            if (e.Key is Key.Enter) /* Then */ action((sender as TextBox).Text);
        }

        private void SearchAlbums_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ScrollEndReached(e))
            {
                (DataContext as DownloadPromptModel).AddAlbums();
            }
        }

        private void SearchSongs_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ScrollEndReached(e))
            { 
                (DataContext as DownloadPromptModel).AddSongs();
            }
        }

        private static bool ScrollEndReached(ScrollChangedEventArgs e)
            => e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight * .9;

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
            => (DataContext as DownloadPromptModel).SelectedItem = (e.Source as ListBoxItem).DataContext as BaseItem;

        private void MenuItem_OnClick_Play(object sender, RoutedEventArgs e)
            => (DataContext as DownloadPromptModel).Play((sender as FrameworkElement).DataContext as SongFileItem);

        private void MenuItem_OnClick_Stream(object sender, RoutedEventArgs e)
            => (DataContext as DownloadPromptModel).StreamMusic(GetItemFromMenu(sender) as Song);

        private void MenuItem_OnClick_Preview(object sender, RoutedEventArgs e)
            => (DataContext as DownloadPromptModel).Preview(GetItemFromMenu(sender));

        private void MenuItem_OnClick_Download(object sender, RoutedEventArgs e)
            => (DataContext as DownloadPromptModel).Download(GetItemFromMenu(sender) as DownloadItem);
        private void MenuItem_OnClick_Delete(object sender, RoutedEventArgs e)
            => (DataContext as DownloadPromptModel).RemoveFile((sender as FrameworkElement).DataContext as SongFileItem);

        private static BaseItem GetItemFromMenu(object sender)
            => (sender as FrameworkElement).DataContext as BaseItem;
    }
}
