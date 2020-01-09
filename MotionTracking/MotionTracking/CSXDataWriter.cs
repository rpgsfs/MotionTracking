using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Numerics;
namespace MotionTracking
{
    class CSXDataWriter
    {
        bool dataaWritten = false; // we only have data logged once.
        public CSXDataWriter()
        {
            
        }
        public void WriteCSX(Vector3[] jointPositions, Quaternion[] jointQuaternions, double[] timestamps)
        {
            if (!dataaWritten)
            {
                var csv = new StringBuilder();
                var title = string.Format("{0},{1},{2}, {3}, {4},{5},{6},{7},{8}", "timestamp", "jointX", "jointY", "jointZ", "Length", "qX", "qY", "qZ", "qW");
                csv.AppendLine(title);
                for (int i = 0; i < jointPositions.Length; i++)
                {
                    var timestamp = timestamps[i];
                    var jointPosition = jointPositions[i];
                    var jointQuaternion = jointQuaternions[i];
                    var jointX = jointPosition.X.ToString();//reader[0].ToString();
                    var jointY = jointPosition.Y.ToString();//image.ToString();
                    var jointZ = jointPosition.Z.ToString();
                    var magn = jointPosition.Length();
                    var jointQX = jointQuaternion.X;
                    var jointQY = jointQuaternion.Y;
                    var jointQZ = jointQuaternion.Z;
                    var jointQW = jointQuaternion.W;
                    var newLine = string.Format("{0},{1},{2}, {3},{4},{5},{6},{7},{8}", timestamp, jointX, jointY, jointZ, magn, jointQX, jointQY, jointQZ, jointQW);
                    csv.AppendLine(newLine);
                }

                File.WriteAllText("test.csv", csv.ToString());
                dataaWritten = true;
                Console.WriteLine("TERMINATE From CSX");
                Environment.Exit(1);
            }
        }
    }
}
