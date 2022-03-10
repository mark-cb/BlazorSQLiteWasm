﻿namespace BlazorSQLiteWasm.Pages;

using Data;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class Demo
{
    public const string SqliteDbFilename = "DemoData.db";

    private string _version = "unknown";

    private readonly List<Car> _cars = new();

    [Inject]
    private IJSRuntime _js { get; set; }

    [Inject]
    private IDbContextFactory<ClientSideDbContext> _dbContextFactory { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // create seed data
        var cars = new[]
            {
        new Car { Brand = "Audi", Price = 21000.999 },
        new Car { Brand = "Volvo", Price = 11000.911 },
        new Car { Brand = "Range Rover", Price = 135000.999 },
        new Car { Brand = "Ford", Price = 8995.911 }
      };

        // EF CORE
        // await using var db = await _dbContextFactory.CreateDbContextAsync();
        // await db.Database.EnsureCreatedAsync();

        //if (!db.Cars.Any())
        //{
        //    await db.Cars.AddRangeAsync(cars);
        //}

        //await Update(db);

        // sqlite-net
        var databasePath = $"Data Source={SqliteDbFilename}";

        var db = new SQLiteConnection(databasePath);
        db.CreateTable<Car>();
        AddCars(db, cars);
        var query = db.Table<Car>().ToList();
        _cars.AddRange(query);
        await base.OnInitializedAsync();
    }

    private async Task SQLiteVersion()
    {
        await using var db = new SqliteConnection($"Data Source={SqliteDbFilename}");
        await db.OpenAsync();
        await using var cmd = new SqliteCommand("SELECT SQLITE_VERSION()", db);
        _version = (await cmd.ExecuteScalarAsync())?.ToString();
    }

    private async Task Create(Car upCar)
    {
        var db = await _dbContextFactory.CreateDbContextAsync();
        await db.Cars.AddAsync(upCar);
        await Update(db);
    }

    private async Task Update(Car upCar)
    {
        var db = await _dbContextFactory.CreateDbContextAsync();
        var car = await db.Cars.FindAsync(upCar.Id);
        car.Brand = upCar.Brand;
        car.Price = upCar.Price;
        db.Cars.Update(car);
        await Update(db);
    }

    private async Task Delete(int id)
    {
        var db = await _dbContextFactory.CreateDbContextAsync();
        var car = await db.Cars.FindAsync(id);
        db.Cars.Remove(car);
        await Update(db);
    }

    private async Task Update(ClientSideDbContext db)
    {
        await db.SaveChangesAsync();
        _cars.Clear();
        _cars.AddRange(db.Cars);
        StateHasChanged();
    }

    public static void AddCars(SQLiteConnection db, Car[] cars)
    {
        //db.Insert(cars);        
        db.Execute($"insert into Car(Brand, Price) values (?,?)", cars[0].Brand, cars[0].Price);
    }
}
