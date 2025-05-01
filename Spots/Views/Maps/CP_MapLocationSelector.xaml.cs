using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace eatMeet;

public partial class CP_MapLocationSelector : ContentPage
{
	private Func<MapSpan?> _MapSpanGetter;
    private bool _FieldsEnabled = true;

    public CP_MapLocationSelector(Func<MapSpan?> mapSpanGetter, string address = "")
	{
		InitializeComponent();

		_MapSpanGetter = mapSpanGetter;
        _lblAddress.Text = address;

        MapSpan? mapSpan = _MapSpanGetter();
        if(mapSpan == null)
        {
            return;
        }

        _cvMap.MoveToRegion(mapSpan);
        _cvMap.Pins.Clear();
        _cvMap.Pins.Add(new Pin()
        {
            Label = address,
            Location = mapSpan.Center
        });

        //_cvMap.MapClicked += _cvMap_MapClicked;
	}
}