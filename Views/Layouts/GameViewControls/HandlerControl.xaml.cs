using Playnite.SDK.Controls;
using PlayniteSounds.Views.Models.GameViewControls;
using System;

namespace PlayniteSounds.Views.Layouts.GameViewControls
{
    public partial class HandlerControl : PluginUserControl
    {
        public HandlerControl()
        {
            InitializeComponent();
            _model = new Lazy<PlayerControlModel>(() => DataContext as PlayerControlModel);
        }

        private readonly Lazy<PlayerControlModel> _model;
        public PlayerControlModel Model => _model.Value;
    }
}
