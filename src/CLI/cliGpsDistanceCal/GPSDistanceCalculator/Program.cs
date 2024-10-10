using System;

public class GpsDistanceCalculator
{
    // 지구의 반지름 (단위: km)
    private const double EarthRadius = 6371.0;

    public static void Main(string[] args)
    {
        // 두 지점의 위도, 경도, 고도 (단위: 도, 도, 미터)
        double lat1 = 37.7749;
        double lon1 = -122.4194;
        double alt1 = 30.0;

        double lat2 = 34.0522;
        double lon2 = -118.2437;
        double alt2 = 100.0;

        double distance = CalculateDistance(lat1, lon1, alt1, lat2, lon2, alt2);
        Console.WriteLine($"두 지점 간의 거리: {distance} km");
    }

    public static double CalculateDistance(double lat1, double lon1, double alt1, double lat2, double lon2, double alt2)
    {
        // 위도와 경도를 라디안으로 변환
        double lat1Rad = DegreesToRadians(lat1);
        double lon1Rad = DegreesToRadians(lon1);
        double lat2Rad = DegreesToRadians(lat2);
        double lon2Rad = DegreesToRadians(lon2);

        // Haversine 공식
        double dLat = lat2Rad - lat1Rad;
        double dLon = lon2Rad - lon1Rad;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double flatDistance = EarthRadius * c;

        // 고도 차이 계산
        double heightDifference = (alt2 - alt1) / 1000.0; // 미터를 km로 변환

        // 3D 거리 계산
        double distance = Math.Sqrt(flatDistance * flatDistance + heightDifference * heightDifference);
        return distance;
    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
