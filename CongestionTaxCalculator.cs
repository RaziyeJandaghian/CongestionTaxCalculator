using System;
using System.Collections.Generic;
using congestion.Vehicle;
using System.Data.SqlClient;

public class CongestionTaxCalculator
{


    private const int MaxDailyFee = 60; // Maximum daily fee
    private const int MaxTimeDifference = 60; // Maximum time difference in minutes
    private const string ConnectionString = "Server=congestiontax_server;Database=congestiontaxGothenburg_db;User Id=admin;Password=Calculator;"; // SQL Server connection string

    /**
         * Constructor
         */
    public CongestionTaxCalculator()
    {
        // Ensure the database is setup with required tables and data
        SetupDatabase();
    }

    /**
         * Setup the database with required tables and initial data
        */
    private void SetupDatabase()
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            // Create tables
            command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Vehicles' AND xtype='U')
                    CREATE TABLE Vehicles (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Type NVARCHAR(50) NOT NULL
                    );

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TollFees' AND xtype='U')
                    CREATE TABLE TollFees (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        StartTime TIME NOT NULL,
                        EndTime TIME NOT NULL,
                        Fee INT NOT NULL
                    );

                    IF NOT EXISTS (SELECT * FROM Vehicles WHERE Type='Motorcycle')
                    INSERT INTO Vehicles (Type) VALUES 
                        ('Motorcycle'),
                        ('Busses'),
                        ('Emergency'),
                        ('Diplomat'),
                        ('Foreign'),
                        ('Military'),
                        ('Car');

                    IF NOT EXISTS (SELECT * FROM TollFees WHERE StartTime='06:00')
                    INSERT INTO TollFees (StartTime, EndTime, Fee) VALUES 
                        ('06:00', '06:29', 8),
                        ('06:30', '06:59', 13),
                        ('07:00', '07:59', 18),
                        ('08:00', '08:29', 13),
                        ('08:30', '14:59', 8),
                        ('15:00', '15:29', 13),
                        ('15:30', '16:59', 18),
                        ('17:00', '17:59', 13),
                        ('18:00', '18:29', 8),
                        ('18:30', '05:59', 0);
                ";
            command.ExecuteNonQuery();
            connection.Close();
        }
    }

    /**
         * Calculate the total toll fee for one day
         *
         * @param vehicle - the vehicle
         * @param dates   - date and time of all passes on one day
         * @return - the total congestion tax for that day
         */

    public int GetTax(Vehicle vehicle, DateTime[] dates)
    {
        if (dates == null || dates.Length == 0) return 0;

        Array.Sort(dates); // Ensure dates are sorted to apply single charge rule

        DateTime intervalStart = dates[0]; // Initialize the start of the interval

        int totalFee = 0; // Initialize the total fee
        
        foreach (DateTime date in dates)
        {
            int nextFee = GetTollFee(date, vehicle); // Get the toll fee for the current date and vehicle

            int tempFee = GetTollFee(intervalStart, vehicle); // Get the toll fee for the interval start date and vehicle

            double minutes = (date - intervalStart).TotalMinutes; // Calculate the time difference in minutes 

            // Check if the time difference is less than or equal to MaxTimeDifference
            if (minutes <= MaxTimeDifference)
            {
                // If there's already a fee, subtract the previous fee from the total fee
                if (totalFee > 0) totalFee -= tempFee;
                // If the next fee is higher than the current fee, update the current fee
                if (nextFee >= tempFee) tempFee = nextFee;
                // updated the total fee
                totalFee += tempFee;
            }
            else
            {
                // updated the total fee
                totalFee += nextFee;
                // Update the IntervalStart for the next iteration
                intervalStart = date;
            }

        }
        // Cap the total fee at MaxDailyFee
        return Math.Min(totalFee, MaxDailyFee);
    }

    /**
     * Check if the Vehicle is toll-free
     *
     * @param vehicle - the vehicle
     * @return - the state of the vehicle
     */
    private bool IsTollFreeVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return false;
        return GetTollFreeVehicleTypes().Contains(vehicle.GetVehicleType());
    }

    /**
     * Get the list of toll-free vehicle types from the database
     *
     * @return - list of toll-free vehicle types
     */
    private HashSet<string> GetTollFreeVehicleTypes()
    {
        var tollFreeVehicleTypes = new HashSet<string>();
        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            var command = new SqlCommand("SELECT Type FROM Vehicles WHERE Type != 'Car'", connection);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tollFreeVehicleTypes.Add(reader.GetString(0));
                }
            }
            connection.Close();
        }
        return tollFreeVehicleTypes;
    }

    /**
         * Get the toll fee for a specific date and vehicle
         *
         * @param vehicle - the vehicle
         * @param date   - date and time of all passes on one day
         * @return - toll fee for a specific date and vehicle
         */
    public int GetTollFee(DateTime date, Vehicle vehicle)
    {
        if (IsTollFreeDate(date) || IsTollFreeVehicle(vehicle)) return 0;

        using (var connection = new SqlConnection(ConnectionString))
        {
            connection.Open();
            var command = new SqlCommand("SELECT Fee FROM TollFees WHERE StartTime <= @time AND EndTime >= @time", connection);
            command.Parameters.AddWithValue("@time", date.ToString("HH:mm"));
            var fee = (int)command.ExecuteScalar();
            connection.Close();
            return fee;
        }
    }
    // Check if the date is toll-free
    /**
         * Check if the date is toll-free
         *
         * @param date   - date and time of all passes on one day
         * @return - state of the date if it is TollFree or not
         */
    private bool IsTollFreeDate(DateTime date)
    {
        return date.Year == 2013 && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday || IsTollFreeHoliday(date));
    }

    /**
     * Check if the date is a toll-free holiday
     *
     * @param date   - date and time of all passes on one day
     * @return - state of the date if it is holiday or not
     */
    private bool IsTollFreeHoliday(DateTime date)
    {
        return date.Year == 2013 && (date.Month == 1 && date.Day == 1 ||
                                     date.Month == 3 && (date.Day == 28 || date.Day == 29) ||
                                     date.Month == 4 && (date.Day == 1 || date.Day == 30) ||
                                     date.Month == 5 && (date.Day == 1 || date.Day == 8 || date.Day == 9) ||
                                     date.Month == 6 && (date.Day == 5 || date.Day == 6 || date.Day == 21) ||
                                     date.Month == 7 ||
                                     date.Month == 11 && date.Day == 1 ||
                                     date.Month == 12 && (date.Day == 24 || date.Day == 25 || date.Day == 26 || date.Day == 31));
    }
    // Tollfree vehicles set
    private static readonly HashSet<string> TollFreeVehicleTypes = new HashSet<string>
    {
        "Motorcycle",
        "Busses",
        "Emergency",
        "Diplomat",
        "Foreign",
        "Military"
    };
}