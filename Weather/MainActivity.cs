using System;
using System.Json;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Locations;
using Android.Util;

namespace Weather
{
	[Activity (Label = "Parking Locator", MainLauncher = true, Icon = "@drawable/ic_launcher")]
	public class MainActivity : Activity, ILocationListener
	{
		LocationManager locMgr;
		string tag = "MainActivity";
		string api_KEY = "76e253a5c51ecf1dbf17e9ea6b9d6a2f";
		Button getLoc;
		Button getSpot;
		SeekBar distance;
		TextView loc;
		TextView total;
		TextView open;
		TextView dist;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			Log.Debug (tag, "OnCreate called");

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			getLoc = FindViewById<Button> (Resource.Id.getLocation);
			getSpot = FindViewById<Button> (Resource.Id.getSpots);
			loc = FindViewById<TextView> (Resource.Id.locationText);
			open = FindViewById<TextView> (Resource.Id.open);
			total = FindViewById<TextView> (Resource.Id.total);
			distance = FindViewById<SeekBar>(Resource.Id.distance);
			dist = FindViewById<TextView>(Resource.Id.dist);
			dist.Text = "5 miles";

			distance.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) => {
				if (e.FromUser)
				{
					string prog = string.Format("{0}", e.Progress);
					dist.Text = (prog == "1") ? prog + " mile" : prog + " miles";
				}	
			};

			getSpot.Click += delegate {
				int ct = 0;
				Dictionary<string, char> senIn = new Dictionary<string, char>();
				SensorData(ct, senIn);
			};

			var request = HttpWebRequest.Create (string.Format (@"http://api.landscape-computing.com/nboxws/rest/v1/zone/lg_1/?key=" + api_KEY));
			request.ContentType = "application/json";
			request.Method = "GET";

			/*using (HttpWebResponse response = request.GetResponse () as HttpWebResponse) {
				if (response.StatusCode != HttpStatusCode.OK)
					Log.Debug (tag, "Error fetching data. Server returned status code: {0}", response.StatusCode);
				using (StreamReader reader = new StreamReader (response.GetResponseStream ())) {
					var content = reader.ReadToEnd ();
					if (string.IsNullOrWhiteSpace (content)) {
						Log.Debug (tag, "Response contained empty body...");
					} else {
						Log.Debug (tag, "Response Body: \r\n {0}", content);
					}
				}
			}*/
		}

		protected void SensorData (int count, Dictionary<string, char> sensorInfo)
		{
			var request1 = HttpWebRequest.Create (string.Format (@"http://api.landscape-computing.com/nboxws/rest/v1/site/lg/query/summary/?key=" + api_KEY));
			request1.ContentType = "application/json";
			request1.Method = "GET";

			using (HttpWebResponse response1 = request1.GetResponse () as HttpWebResponse) {
				if (response1.StatusCode != HttpStatusCode.OK)
					Log.Debug (tag, "Error fetching data. Server returned status code: {0}", response1.StatusCode);
				using (StreamReader reader = new StreamReader (response1.GetResponseStream ())) {
					var content = reader.ReadToEnd ();
					if (string.IsNullOrWhiteSpace (content)) {
						Log.Debug (tag, "Response contained empty body...");
					} else {
						var occ = content.Split('|');
						for (int i = 0; i < occ.Length; i++) {
							var sensorId = occ[i].Split(':')[0];
							if (!string.IsNullOrWhiteSpace(sensorId)) {
								var occupied = occ[i].Split(':')[1][1];
								if (occupied == '1')
									count++;
								sensorInfo.Add(sensorId, occupied);
							} else	{
								continue;
							}
						}
					}
				}
			}
			getSpot.Text = "Refresh";
			total.Text = sensorInfo.Count().ToString();
			open.Text = count.ToString();
		}

		protected override void OnStart ()
		{
			base.OnStart ();
			Log.Debug (tag, "OnStart called");
		}

		// OnResume gets called every time the activity starts, so we'll put our RequestLocationUpdates
		// code here, so that 
		protected override void OnResume ()
		{
			base.OnResume (); 
			Log.Debug (tag, "OnResume called");

			// initialize location manager
			locMgr = GetSystemService (Context.LocationService) as LocationManager;

			getLoc.Click += delegate {
				// pass in the provider (GPS), 
				// the minimum time between updates (in seconds), 
				// the minimum distance the user needs to move to generate an update (in meters),
				// and an ILocationListener (recall that this class impletents the ILocationListener interface)
				if (locMgr.AllProviders.Contains (LocationManager.NetworkProvider)
					&& locMgr.IsProviderEnabled (LocationManager.NetworkProvider)) {
					getLoc.Text = "Getting location...";
					locMgr.RequestLocationUpdates (LocationManager.NetworkProvider, 2000, 1, this);
				} else {
					Toast.MakeText (this, "Please enable location services.", ToastLength.Long).Show ();
				}
			};
		}

		protected override void OnPause ()
		{
			base.OnPause ();
			// RemoveUpdates takes a pending intent - here, we pass the current Activity
			locMgr.RemoveUpdates (this);
			Log.Debug (tag, "Location updates paused because application is entering the background");
		}

		protected override void OnStop ()
		{
			base.OnStop ();
			Log.Debug (tag, "OnStop called");
		}

		public void OnLocationChanged (Android.Locations.Location location)
		{
			Log.Debug (tag, "Location changed");
			var lat = location.Latitude;
			var lng = location.Longitude;
			var geo = new Geocoder (this);
			List<Address> getAddress = new List<Address>(geo.GetFromLocation(lat,lng,1));
			Address returnedAddress = getAddress.FirstOrDefault();
			if (returnedAddress != null) {
				System.Text.StringBuilder strReturnedAddress = new StringBuilder ();
				for (int i = 0; i < returnedAddress.MaxAddressLineIndex; i++) {
					strReturnedAddress.Append (returnedAddress.GetAddressLine (i)).AppendLine (" ");
				}
				loc.Text = strReturnedAddress.ToString ();
				getLoc.Text = "My Location";
				//determine whether closer to los gatos or palo alto
			}
		}
		public void OnProviderDisabled (string provider)
		{
			Log.Debug (tag, provider + " disabled by user");
		}
		public void OnProviderEnabled (string provider)
		{
			Log.Debug (tag, provider + " enabled by user");
		}
		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
			Log.Debug (tag, provider + " availability has changed to " + status.ToString());
		}
	}	
}


