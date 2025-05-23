﻿using System;
using Npgsql;

class Program
{
    static void Main()
    {
        // PostgreSQL 데이터베이스 연결 문자열
        //var connString = "Host=127.0.0.1;Username=parksanghoon;Password=tjb4048796;Database=fullstackhero";
        var connString = "Server=shpark94.synology.me;Database=postgres;Port=25432;User Id=postgres;Password=parktest1#;";

        using (var conn = new NpgsqlConnection(connString))
        {
            conn.Open();

            // 데이터베이스에서 쿼리 실행
            using (var cmd = new NpgsqlCommand("SELECT version()", conn))
            {
                var version = cmd.ExecuteScalar().ToString();
                Console.WriteLine($"PostgreSQL Version: {version}");
            }
        }
    }
}
