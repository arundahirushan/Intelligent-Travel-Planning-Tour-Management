using System;
using System.Linq;
using TourManagement.Api.Data;
using TourManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

public static class TestQuery
{
    public static void Run(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(builder.Configuration["ConnectionStrings:DefaultConnection"]));
        var app = builder.Build();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var contracts = db.Contracts.ToList();
        Console.WriteLine($"Total Contracts: {contracts.Count}");
        foreach(var c in contracts) {
            Console.WriteLine($"ID: {c.Id}, SupplierId: {c.SupplierId}, Status: {c.Status}, EndDate: {c.EndDate}");
        }

        var requests = db.ContractRequests.ToList();
        Console.WriteLine($"Total Requests: {requests.Count}");
        foreach(var r in requests) {
            Console.WriteLine($"ID: {r.Id}, SupplierId: {r.SupplierId}, Status: {r.Status}");
        }
    }
}
