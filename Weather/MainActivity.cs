using System;
using System.Json;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Collections.Generic;

using Xamarin.Forms.Labs.Services.Geolocation;

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
		bool town;
		Position pos1 = new Xamarin.Forms.Labs.Services.Geolocation.Position();
		double radius = 0.5;

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
			dist.Text = "0.5 miles";
			getSpot.Enabled = false; 

			distance.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) => {
				if (e.FromUser)
				{
					radius = Convert.ToDouble(e.Progress) / 10.0;
					string prog = string.Format("{0}", radius);
					dist.Text = (prog == "1") ? prog + " mile" : prog + " miles";
				}	
			};

			getSpot.Click += delegate {
				int ct = 0;
				Dictionary<string, string> senIn = new Dictionary<string, string>();
				Dictionary<string, string> senCoord = new Dictionary<string, string>();
				SensorData(radius, pos1, ct, senIn, senCoord, town);
			};
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
					//set alert for executing the task
					AlertDialog.Builder alert = new AlertDialog.Builder (this);
					alert.SetTitle ("Please enable location services");
					alert.SetNeutralButton ("OK", (senderAlert, args) => {} );
					//run the alert in UI thread to display in the screen
					RunOnUiThread (() => {
						alert.Show();
					});
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
				pos1 = new Xamarin.Forms.Labs.Services.Geolocation.Position()
				{
					Latitude = location.Latitude,
					Longitude = location.Longitude
				};


				//determine whether closer to los gatos or palo alto
				if (loc.Text.Contains("Palo Alto")) {
					town = false;
					getSpot.Enabled = true;
				} else if (loc.Text.Contains("Los Gatos")) {
					town = true;
					getSpot.Enabled = true;
				} else {
					//set alert for executing the task
					AlertDialog.Builder alert = new AlertDialog.Builder (this);
					alert.SetTitle ("Sorry, you are too far from Palo Alto or Los Gatos");
					alert.SetNeutralButton ("OK", (senderAlert, args) => {} );
					//run the alert in UI thread to display in the screen
					RunOnUiThread (() => {
						alert.Show();
					});
				}
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

		protected void SensorData (double rad, Position pos, int count, Dictionary<string, string> sensorInfo, Dictionary<string, string> sensorCoord, bool city)
		{
			string place = (!city) ? "pa" : "lg";
			string zone = "1";
			var request1 = HttpWebRequest.Create (string.Format (@"http://api.landscape-computing.com/nboxws/rest/v1/site/" + place + "/query/summary/?key=" + api_KEY));
			request1.ContentType = "application/json";
			request1.Method = "GET";

			using (HttpWebResponse response1 = request1.GetResponse () as HttpWebResponse) {
				if (response1.StatusCode != HttpStatusCode.OK)
					Log.Debug (tag, "Error fetching data. Server returned status code: {0}", response1.StatusCode);
				using (StreamReader reader1 = new StreamReader (response1.GetResponseStream ())) {
					var content1 = reader1.ReadToEnd ();
					if (string.IsNullOrWhiteSpace (content1)) {
						Log.Debug (tag, "Response contained empty body...");
					} else {
						var occ = content1.Split('|');
						for (int i = 0; i < occ.Length; i++) {
							var sensorId = occ[i].Split(':')[0];
							if (!string.IsNullOrWhiteSpace(sensorId)) {
								var occupied = Char.ToString(occ[i].Split(':')[1][1]);
								if (occupied == "0") {
									count++;
								}
								sensorInfo.Add(sensorId, occupied);
							} else	{
								continue;
							}
						}
					}
				}
			}
			var request = HttpWebRequest.Create (string.Format (@"http://api.landscape-computing.com/nboxws/rest/v1/zone/" + place + "_" + zone + "/?key=" + api_KEY));
			request.ContentType = "application/json";
			request.Method = "GET";

			using (HttpWebResponse response = request.GetResponse () as HttpWebResponse) {
				if (response.StatusCode != HttpStatusCode.OK)
					Log.Debug (tag, "Error getting data. Server returned status code: {0}", response.StatusCode);
				using (StreamReader reader = new StreamReader (response.GetResponseStream ())) {
					string content = reader.ReadToEnd ();
					if (string.IsNullOrWhiteSpace (content)) {
						Log.Debug (tag, "Response contained empty body...");
					} else {
						var test = content.Split(new string[] { "sensorId" }, StringSplitOptions.None);
						for (int i = 1; i < test.Length - 1; i++) {
							string temp = test[i];
							if (temp.Contains("guid")) {
								int sFrom = temp.IndexOf("<guid>") + "<guid>".Length;
								int sTo = temp.LastIndexOf("</guid>");
								string senseId = temp.Substring(sFrom, sTo - sFrom);
								int gFrom = temp.IndexOf("<gpsCoord>") + "<gpsCoord>".Length;
								int gTo = temp.IndexOf("</gpsCoord>");
								string coordinates = temp.Substring (gFrom, gTo - gFrom);
								sensorCoord.Add (senseId, coordinates);
							}
						}
					}
				}
			}

			Dictionary<string, List<double>> dict = new Dictionary<string, List<double>>();
			foreach (var sensor in sensorCoord) {
				if (sensorInfo.ContainsKey (sensor.Key)) {
					List<double> vals = new List<double>();
					double occ = Double.Parse(sensorInfo [sensor.Key]);
					if (occ == 0) {
						string latSt = sensor.Value.Split (',') [0];
						string lngSt = sensor.Value.Split (',') [1];
						double lat = 0;
						double lng = 0;
						Double.TryParse (latSt, out lat);
						Double.TryParse (lngSt, out lng);
						float[] distance = new float[1];
						Location.DistanceBetween(pos.Latitude, pos.Longitude, lat, lng, distance);
						double dist = System.Convert.ToDouble(distance[0]) / 1609.344;
						if (dist <= rad) {
							Log.Debug (tag, "{0}", dist);
							vals.Add (occ);
							vals.Add (lat);
							vals.Add (lng);
							dict.Add (sensor.Key, vals);
						} else
							Log.Debug (tag, "Not {0}", dist);
					} else
						continue;
				} else
					Log.Debug (tag, "what");
			}

			foreach (var sensor in dict) {
				Log.Debug (tag, "{0}, {1}", sensor.Key, sensor.Value [0]);
				/*foreach (var value in sensor.Value) {
					Log.Debug (tag, "{0}: {1}", sensor.Key, value);
				}*/
			}

			getSpot.Text = "Refresh";
			total.Text = sensorInfo.Count().ToString();
			open.Text = dict.Count.ToString();
		}
	}	
}


