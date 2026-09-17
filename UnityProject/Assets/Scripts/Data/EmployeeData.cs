using System;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Employee role types.
    /// Mirrors backend EmployeeRole enum.
    /// </summary>
    [Serializable]
    public enum EmployeeRole
    {
        cashier,
        manager,
        chef,
        cleaner
    }

    /// <summary>
    /// Employee performance stats.
    /// Mirrors backend EmployeeStats model.
    /// </summary>
    [Serializable]
    public class EmployeeStats
    {
        public int speed = 50;
        public int accuracy = 50;
        public int customer_care = 50;
        public int experience_level = 0;

        public int CalculateOverallRating()
        {
            return (speed + accuracy + customer_care) / 3;
        }
    }

    /// <summary>
    /// Employee model.
    /// Mirrors backend Employee model.
    /// </summary>
    [Serializable]
    public class Employee
    {
        public string employee_id;
        public string name;
        public string role;
        public EmployeeStats stats;
        public float salary_per_day;
        public string hired_at;

        // Convenience properties
        public EmployeeRole RoleEnum
        {
            get
            {
                if (Enum.TryParse<EmployeeRole>(role, out var result))
                    return result;
                return EmployeeRole.cashier;
            }
        }

        public Employee()
        {
            stats = new EmployeeStats();
        }

        public override string ToString()
        {
            return $"{name} ({role}) | Salary: ₹{salary_per_day}/day | Rating: {stats.CalculateOverallRating()}";
        }
    }

    /// <summary>
    /// Request to hire a new employee.
    /// </summary>
    [Serializable]
    public class EmployeeHireRequest
    {
        public string name;
        public string role;

        public EmployeeHireRequest(string employeeName, string employeeRole)
        {
            name = employeeName;
            role = employeeRole;
        }
    }
}
