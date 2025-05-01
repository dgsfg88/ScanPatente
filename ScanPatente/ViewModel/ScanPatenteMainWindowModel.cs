using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAPS2.Images;
using NAPS2.Images.Wpf;
using NAPS2.Scan;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScanPatente.ViewModel
{
	internal partial class ScanPatenteMainWindowModel : ObservableObject, IDisposable
	{
		[ObservableProperty]
		private ObservableCollection<ScanDevice> devices = new ObservableCollection<ScanDevice>();
		[ObservableProperty]
		private ScanDevice? selectedDevice = default;
		[ObservableProperty]
		private bool isUIEnabled = true;
		[ObservableProperty]
		private BitmapSource? imageToShow = null;
		private ScanningContext scanningContext;
		private ScanController controller;
		private CancellationTokenSource devicesUpdateCancellationToken;
		private bool disposedValue;

		public ScanPatenteMainWindowModel()
		{
			// Set up
			scanningContext = new ScanningContext(new NAPS2.Images.Wpf.WpfImageContext());
			controller = new ScanController(scanningContext);
			devicesUpdateCancellationToken = new CancellationTokenSource();

			Task.Run(async () => await PeriodicDeviceUpdateAsync(
				new TimeSpan(0, 0, 0, 1), devicesUpdateCancellationToken.Token),
				devicesUpdateCancellationToken.Token);
		}

		public async Task PeriodicDeviceUpdateAsync(TimeSpan interval, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				// Query for available scanning devices
				var devices = await controller.GetDeviceList();
				if (devices != null)
				{
					var toRemove = Devices.Except(devices).ToList();
					var toAdd = devices.Except(Devices).ToList();

					foreach (var item in toRemove)
						Devices.Remove(item);
					foreach (var item in toAdd)
						Devices.Add(item);

					if (SelectedDevice is null)
						SelectedDevice = Devices.FirstOrDefault();
				}

				await Task.Delay(interval, cancellationToken);
			}
		}

		[RelayCommand]
		protected async Task Scan()
		{
			// Set scanning options
			var options = new ScanOptions
			{
				Device = SelectedDevice,
				PaperSource = PaperSource.Auto,
				PageSize = PageSize.A4,
				Dpi = 200
			};

			try
			{
				IsUIEnabled = false;

				await foreach (var image in controller.Scan(options))
				{
					//La writeableBitmap viene inizializzata nel thread della scansione, quindi
					//	è necessario copiarla perché non possiede un dispatcher
					ImageToShow = new WriteableBitmap(((WpfImage)image.Storage).Bitmap);
				}
			}
			catch (Exception ex)
			{
				//TODO
			}
			finally
			{
				IsUIEnabled = true;
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
					devicesUpdateCancellationToken.Cancel();
					devicesUpdateCancellationToken.Dispose();
					scanningContext.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~ScanPatenteMainWindowModel()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
