using System;
using System.Text.RegularExpressions;
using DynamoDBAutoScale.Enumerations;

namespace DynamoDBAutoScale
{
	public class Measurement
	{
		public MeasurementTypes measurement_type { get; set; }
		public long measurement_units { get; set; }
		public byte measurement_percentage { get; set; }

		public Measurement(Measurement measurement)
		{
			this.measurement_type = measurement.measurement_type;
			this.measurement_units = measurement.measurement_units;
			this.measurement_percentage = measurement.measurement_percentage;
		}

		public Measurement(string measurement)
		{
			this.measurement_type = MeasurementTypes.Percentage;
			this.measurement_units = 0;
			this.measurement_percentage = 0;

			if (!string.IsNullOrWhiteSpace(measurement))
			{
				try
				{
					measurement = Regex.Replace(measurement, @"\s+", string.Empty);
					if (measurement.EndsWith("%"))
					{
						measurement_type = MeasurementTypes.Percentage;
						measurement = measurement.Replace("%", string.Empty);
						measurement_percentage = byte.Parse(measurement);
					}
					else
					{
						measurement_type = MeasurementTypes.Units;
						measurement_units = long.Parse(measurement);
					}
				}
				catch (Exception exception)
				{
					measurement_type = MeasurementTypes.Percentage;
					measurement_units = 0;
					measurement_percentage = 0;
					// provide custom exception handling here
				}
			}
		}
	}
}