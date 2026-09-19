using System;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Employee role types.
    /// Mirrors backend EmployeeRole enum exactly.
    /// Phase 3: Added cleaner role.
    /// </summary>
    [Serializable]
    public enum EmployeeRole
    {
        cashier,
        restocker,  // Phase 2 (was missing!)
        cleaner     // Phase 3
    }

    /// <summary>
    /// Employee performance stats.
    /// Mirrors backend EmployeeStats model exactly.
    /// Phase 3: Added carry_capacity.
    /// </summary>
    [Serializable]
    public class EmployeeStats
    {
        public int speed          = 50;
        public int accuracy       = 50;
        public int customer_care  = 50;
        public int carry_capacity = 5;   

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
        public string        employee_id;
        public string        name;
        public string        role;
        public EmployeeStats stats;
        public float         salary_per_day;
        public int           experience;
        public int           level;
        public string        hired_at;
        public string        business_id;

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

    /// <summary>
    /// Phase 3: Request to upgrade a specific employee stat.
    /// Mirrors backend EmployeeUpgradeRequest.
    /// Valid values for stat: "speed" or "carry_capacity"
    /// </summary>
    [Serializable]
    public class EmployeeUpgradeRequest
    {
        public string stat;

        public EmployeeUpgradeRequest(string statName)
        {
            stat = statName;
        }
    }
}
