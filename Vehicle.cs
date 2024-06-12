
namespace congestion.Vehicle
{
    // Abstract class for vehicles
    public abstract class Vehicle
    {
        public abstract string GetVehicleType();
    }

    // Class for Car
    public class Car : Vehicle
    {
        public override string GetVehicleType()
        {
            return "Car";
        }
    }

    // Class for Motorcycle
    public class Motorcycle : Vehicle
    {
        public override string GetVehicleType()
        {
            return "Motorcycle";
        }
    }

    // Class for Busses
    public class Busses : Vehicle
    {
        public override string GetVehicleType()
        {
            return "Busses";
        }
    }

    // Class for Emergency vehicles
    public class Emergency : Vehicle
    {
        public override string GetVehicleType()
        {
            return "Emergency";
        }
    }

    // Class for Diplomat vehicles
    public class Diplomat : Vehicle
    {
        public override string GetVehicleType()
        {
            return "Diplomat";
        }
    }

    // Class for Foreign vehicles
    public class Foreign : Vehicle
    {
        public override string GetVehicleType()
        {
            return "Foreign";
        }
    }

    // Class for Military vehicles
    public class Military : Vehicle
    {
        public override string GetVehicleType()
        {
            return "Military";
        }
    }
}