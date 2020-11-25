using EasyModbus;
using System;
using System.Data.SqlClient;
using System.Globalization;

namespace Modbus_RTU_SensorData
{
    class Program
    {
        public static bool status { get; set; }
        static void Main(string[] args)
        {
            status = true;
            startCollecting(status);
            //stopCollecting();
        }
        static void startCollecting(bool status)
        {
            try
            {
                ModbusClient modbusClient = new ModbusClient("COM3");
                modbusClient.Baudrate = 115200;	// Not necessary since default baudrate = 9600
                modbusClient.Parity = System.IO.Ports.Parity.None;
                modbusClient.StopBits = System.IO.Ports.StopBits.Two;
                modbusClient.ConnectionTimeout = 5000;
                modbusClient.Connect();
                Console.WriteLine("Device Connection Successful");

                String[] sensordata = { "ID", "Temp", "Humidity", "Part03", "Part05", "DateTime" };
                Console.WriteLine("Count\t" + string.Join("\t", sensordata) + "\t\t Run Time");

                SqlConnection myConnection = new SqlConnection(@"Data Source=DESKTOP-DLIT;Initial Catalog=SensorDataDB;Integrated Security=True");
                myConnection.Open();
                status = true;
                DateTime startTime = DateTime.Now;
                int dataCount = 0;

                while (status)
                {
                    for (byte j = 1; j < 4; j++)
                    {

                        dataCount += 1;
                        modbusClient.UnitIdentifier = j;

                        //string timestamp0 = now.ToString("yyyy-MM-dd HH:mm:ss.fff"); // 

                        int[] test1 = modbusClient.ReadInputRegisters(10, 6);
                        DateTime timestamp = DateTime.Now;
                        var timeCount = DateTime.Now - startTime;
                        string timestamp0 = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        decimal temp = test1[0];
                        decimal humid = test1[1];
                        int[] part03_arr = { 0, 0 };
                        int[] part05_arr = { 0, 0 };
                        part03_arr[0] = test1[3];
                        part03_arr[1] = test1[2];
                        part05_arr[0] = test1[5];
                        part05_arr[1] = test1[4];
                        Int64 part03 = ModbusClient.ConvertRegistersToInt(part03_arr);
                        Int64 part05 = ModbusClient.ConvertRegistersToInt(part05_arr);
                        Console.WriteLine(dataCount + "\t" + j + "\t" + (temp / 100m).ToString("F", CultureInfo.InvariantCulture) + "\t" + (humid / 100m).ToString("F", CultureInfo.InvariantCulture) + "\t\t" + String.Format("{0:n0}", part03) + "\t" + String.Format("{0:n0}", part05) + "\t" + timestamp0 + "\t  {0}일 {1}시간 {2}분 {3}초", timeCount.Days, timeCount.Hours, timeCount.Minutes, timeCount.Seconds);

                        string sql_str_temp = "INSERT INTO DEV_TEMP_" + j.ToString() + " (Temperature, DateAndTime) Values (@Temperature, @DateAndTime)";
                        string sql_str_humid = "INSERT INTO DEV_HUMID_" + j.ToString() + " (Humidity, DateAndTime) Values (@Humidity, @DateAndTime)";
                        string sql_str_part03 = "INSERT INTO DEV_PART03_" + j.ToString() + " (Particle03, DateAndTime) Values (@Particle03, @DateAndTime)";
                        string sql_str_part05 = "INSERT INTO DEV_PART05_" + j.ToString() + " (Particle05, DateAndTime) Values (@Particle05, @DateAndTime)";

                        SqlCommand myCommand_temp = new SqlCommand(sql_str_temp, myConnection);
                        SqlCommand myCommand_humid = new SqlCommand(sql_str_humid, myConnection);
                        SqlCommand myCommand_part03 = new SqlCommand(sql_str_part03, myConnection);
                        SqlCommand myCommand_part05 = new SqlCommand(sql_str_part05, myConnection);

                        myCommand_temp.Parameters.AddWithValue("@Temperature ", (temp / 100m).ToString("F", CultureInfo.InvariantCulture));
                        myCommand_temp.Parameters.AddWithValue("@DateAndTime", timestamp0);
                        myCommand_temp.ExecuteNonQuery();

                        myCommand_humid.Parameters.AddWithValue("@Humidity", (humid / 100m).ToString("F", CultureInfo.InvariantCulture));
                        myCommand_humid.Parameters.AddWithValue("@DateAndTime", timestamp0);
                        myCommand_humid.ExecuteNonQuery();

                        myCommand_part03.Parameters.AddWithValue("@Particle03", part03);
                        myCommand_part03.Parameters.AddWithValue("@DateAndTime", timestamp0);
                        myCommand_part03.ExecuteNonQuery();

                        myCommand_part05.Parameters.AddWithValue("@Particle05", part05);
                        myCommand_part05.Parameters.AddWithValue("@DateAndTime", timestamp0);
                        myCommand_part05.ExecuteNonQuery();
                    }
                }
                myConnection.Close();
                modbusClient.Disconnect();
                Console.Write("Press any key to continue . . . ");
                Console.ReadKey(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
            static void stopCollecting()
        {
            status = false;
        }
    }
}
