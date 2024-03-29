
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using System.Xml.Linq;
// Define the type for Distance
using Distance = System.Int32;

public record City(string Name)
{
    public Edge[] Edges { get; set; }
}
public record Edge(City ConnectedTo, Distance Distance);

internal class Program
{

    private static void Main(string[] args)
    {
        var sanFran = new City("San Francisco");
        var la = new City("Los Angeles");
        var dallas = new City("Dallas");
        var newYork = new City("New York");
        var chicago = new City("Chicago");

        // Update the cities with the distance to each connected node
        sanFran.Edges = new[]
        {
            new Edge(la, 347),
            new Edge(dallas, 1480),
            new Edge(chicago, 1853)
        };
        la.Edges = new[]
        {
            new Edge(dallas, 1237),
            new Edge(sanFran, 347)
        };
        dallas.Edges = new[]
        {
            new Edge(chicago, 802),
            new Edge(newYork, 1370),
            new Edge(sanFran, 1480),
            new Edge(la, 1237)
        };
        chicago.Edges = new[]
        {
            new Edge(newYork, 712),
            new Edge(sanFran, 1853),
            new Edge(dallas, 802)
        };
        newYork.Edges = new[]
        {
            new Edge(dallas, 1370),
            new Edge(chicago, 712)
        };
        // The full collection of cities
        City[] allCities = { sanFran, la, dallas, newYork, chicago };

        // Find shortest path from San Francisco to New York
        var shortestPath = DijkstraShortestPath(sanFran, newYork, allCities);

        Console.WriteLine("Shortest Path:");
        foreach (var city in shortestPath)
        {
            Console.WriteLine(city.Name, city.Edges);
        }
    }

    private static List<City> DijkstraShortestPath(City start, City end, City[] allCities)
    {

        // Priority queue to store cities based on their tentative distances
        var priorityQueue = new PriorityQueue<City, Distance>();

        // Dictionary to store the tentative distances of cities
        var tentativeDistances = new Dictionary<City, Distance>();

        // Dictionary to store the previous city in the shortest path
        var previous = new Dictionary<City, City>();

        // Initialize tentative distances and add all cities to the priority queue

        foreach (var city in allCities)
        {
            tentativeDistances[city] = int.MaxValue;
            previous[city] = null;
            priorityQueue.Enqueue(city, int.MaxValue);
        }

        tentativeDistances[start] = 0;

        while (priorityQueue.Count > 0)
        {
            var currentCity = priorityQueue.Dequeue();
            if (currentCity.Name == end.Name)
                break;
            foreach (var edge in currentCity.Edges)
            {
                var neighbor = edge.ConnectedTo;
                var distance = edge.Distance;
                var tentativeDistance = tentativeDistances[currentCity] + distance;

                // If this path is shorter than the current known shortest path to the neighbor
                if (tentativeDistance < tentativeDistances[neighbor])
                {
                    tentativeDistances[neighbor] = tentativeDistance;
                    previous[neighbor] = currentCity;

                    // Enqueue the neighbor with updated priority (distance)
                    priorityQueue.Enqueue(neighbor, tentativeDistance);
                }
                else
                {
                    int a = 0;
                }
            }            
        } 
        // Reconstruct the shortest path
        var path = new List<City>();
        var current = end;
        while (current != null)
        {
            path.Add(current);
            current = previous[current];
        }
        path.Reverse();

        return path;
    }
}