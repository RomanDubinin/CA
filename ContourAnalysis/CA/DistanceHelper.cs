namespace ContourAnalysisNS
{
	public static class DistanceHelper
	{
		public static double DistanceByRelation(double relation)
		{
			if (relation < 0.42)
				return
					22243.6*relation*relation*relation*relation -
					25944.1*relation*relation*relation +
					11189.6*relation*relation -
					2200.19*relation +
					195.518;
			return -12.069*relation + 20.069;

		}
	}
}