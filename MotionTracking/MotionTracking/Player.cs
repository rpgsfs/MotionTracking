
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace MotionTracking
{
    class Player    
    {
        public uint BodyID { get; private set; }
        private Skeleton skeleton;
        // private List<Skeleton> OldSkeletons = new List<Skeleton>();
        private readonly int ActionTriggerTolerance = 100;
        private readonly int MaxPastBodiesStored = 150; //30 frames by 5 seconds
        private List<TimestampSkeleton> TimestampSkeletons;  // holds past skeletons with time for motion tracking
        private readonly int CancelThershold = 5;
        private int CancelTriggers = 0;

        private readonly CSXDataWriter CSXDataWriter;

        public bool Updated {  get;  set; }
        public Player(Body body, double timestamp)
        {
            BodyID = body.Id;
            skeleton = body.Skeleton;
            Updated = true;
            TimestampSkeletons = new List<TimestampSkeleton>(MaxPastBodiesStored);
            UpdatePastSkeletons(new TimestampSkeleton(skeleton, timestamp));
            CSXDataWriter = new CSXDataWriter();
        }
        enum JointIndices
        {
            Pelvis,
            SpineNaval,
            SpineChest,
            Neck,
            ClavicleLeft,
            ShoulderLeft,
            ElbowLeft,
            WristLeft,
            HandLeft,
            HandTipLeft,
            ThumbLeft,
            ClavicleRight,
            ShoulderRight,
            ElbowRight,
            WristRight,
            HandRight,
            HandTipRight,
            ThumbRight,
            HipLeft,
            KneeLeft,
            AnkleLeft,
            FootLeft,
            HipRight,
            KneeRight,
            AnkleRight,
            FootRight,
            Head,
            Nose,
            EyeLeft,
            EarLeft,
            EyeRight,
            EarRight
        }
        private struct TimestampSkeleton
        {
            public Skeleton Skeleton;
            public double Timestamp;           
            public TimestampSkeleton(Skeleton skeleton, double timestamp)
            {
                Timestamp = timestamp;
                Skeleton = skeleton;               
            }

        }
        
        public void UpdatePlayer(Body body, double timestamp)
        {
            if (BodyID == body.Id)
            {

                skeleton = body.Skeleton;
                UpdatePastSkeletons(new TimestampSkeleton(skeleton, timestamp));
                Updated = true;

            }
            else throw new InvalidOperationException("Iput Id's must remain constant");
        }
        private void UpdatePastSkeletons(TimestampSkeleton timestampSkeleton)
        {
            if(TimestampSkeletons.Count == 0)
            {
                TimestampSkeletons.Add(timestampSkeleton);
            }
            if(TimestampSkeletons.Count == TimestampSkeletons.Capacity)
            {
                TimestampSkeletons.RemoveAt(TimestampSkeletons.Count - 1);
                TimestampSkeletons.TrimExcess();
            }
            TimestampSkeletons.Insert(0, timestampSkeleton);
          /*  Console.WriteLine("Skeletons: " + TimestampSkeletons.Count);
            Console.WriteLine(TimestampSkeletons[0].Timestamp);*/
        }
        public void LogCSX(int jointEnum) //logs the coordinates of a joint to a csx file
        {
            var timestamps = new double[MaxPastBodiesStored];
            var jointPositions = new Vector3[MaxPastBodiesStored];
            var jointQuaternions = new Quaternion[MaxPastBodiesStored];
            if(TimestampSkeletons.Count == MaxPastBodiesStored)
            {
               for(int i = 0; i < TimestampSkeletons.Count; i++)
                {
                    var joint = TimestampSkeletons[i].Skeleton.GetJoint(jointEnum);
                    timestamps[i] = TimestampSkeletons[i].Timestamp;
                    jointQuaternions[i] = joint.Quaternion;
                    jointPositions[i] = joint.Position;
                }
                CSXDataWriter.WriteCSX(jointPositions, jointQuaternions, timestamps);
                Console.WriteLine("CSX logged");
            }
        }
      
        public void AddCancelTrigger()
        {
            if (RightHandWave())
            {
                CancelTriggers++;
                Console.WriteLine("Cancel triggers" + CancelTriggers);
            }

            if (CancelTriggers >= CancelThershold)
            {
                CancelThresholdEventArgs args = new CancelThresholdEventArgs();
                args.Threshold = CancelThershold;
                OnCancelTrigger(args);
                CancelTriggers = 0;
            }
        }
        protected virtual void OnCancelTrigger(CancelThresholdEventArgs e)
        {
            EventHandler<CancelThresholdEventArgs> handler = CancelThresholdReached;
            if (handler != null)
            {
                handler(this, e);
            }

        }
    
        public bool RightHandWave()
        {

            float speedX = XAxisSpeed((int)JointIndices.HandTipRight);
            float speedY = YAxisSpeed((int)JointIndices.HandTipRight);
            float speedZ = ZAxisSpeed((int)JointIndices.HandTipRight);
            if (speedX > speedY * 2 && speedX > speedZ * 2) //ensure player is not making slight hand adjustments.
            if(skeleton.GetJoint((int)JointIndices.HandTipRight).ConfidenceLevel == JointConfidenceLevel.Medium)
            if (speedX > .6 && speedX < 1.8 || speedX < -.6 && speedX > -1.8)
            if (Math.Abs(speedX) > XAxisSpeed((int)JointIndices.HandRight) + .5) // ensure hand tip is outpivoting the hand
            {
                            Console.WriteLine("CANCEL TICK");
                Console.WriteLine("TIP" + speedX);
                Console.WriteLine("Hand" + XAxisSpeed((int)JointIndices.HandRight));
               // Console.WriteLine("Diff" + )
                    return true;
                            
            }
            return false;
        }
        public bool PlayerMoving()//we do not want the player to trigger events while moving.
        {
            double maximumPlayerSpeed = .2;           
            return TotalSpeed((int)JointIndices.SpineChest) > maximumPlayerSpeed; 
        }
        private float XAxisSpeed(int jointEnum) //could crash first tick
        {
            var lastJointXPosition = TimestampSkeletons[1].Skeleton.GetJoint(jointEnum).Position.X;
            var currentJointXPosition = TimestampSkeletons[0].Skeleton.GetJoint(jointEnum).Position.X;
            var timePassed = TimestampSkeletons[0].Timestamp - TimestampSkeletons[1].Timestamp;       
            return (currentJointXPosition - lastJointXPosition)/(float)timePassed;

        }
        private float YAxisSpeed(int jointEnum) //could crash first tick
        {
            var lastJointYPosition = TimestampSkeletons[1].Skeleton.GetJoint(jointEnum).Position.Y;
            var currentJoinYPosition = TimestampSkeletons[0].Skeleton.GetJoint(jointEnum).Position.Y;
            var timePassed = TimestampSkeletons[0].Timestamp - TimestampSkeletons[1].Timestamp;
            return (currentJoinYPosition - lastJointYPosition) / (float)timePassed;

        }
        private float ZAxisSpeed(int jointEnum) //could crash first tick
        {
            var lastJointYPosition = TimestampSkeletons[1].Skeleton.GetJoint(jointEnum).Position.Z;
            var currentJoinYPosition = TimestampSkeletons[0].Skeleton.GetJoint(jointEnum).Position.Z;
            var timePassed = TimestampSkeletons[0].Timestamp - TimestampSkeletons[1].Timestamp;
            return (currentJoinYPosition - lastJointYPosition) / (float)timePassed;

        }
        private double TotalSpeed(int jointEnum)
        {
            double Xspeed = (double)XAxisSpeed(jointEnum);
            double Yspeed = (double)YAxisSpeed(jointEnum);
            double ZSpeed = (double)ZAxisSpeed(jointEnum);
            return Math.Sqrt(Xspeed * Xspeed + Yspeed * Yspeed + ZSpeed * ZSpeed);
        }
        public double QuaternionComparer(int jointEnum1, int jointEnum2)//TODO switch tp private
        {
            //   return Quaternion.Dot(skeleton.GetJoint(jointEnum1).Quaternion, skeleton.GetJoint(jointEnum2).Quaternion);
            Vector3 j1 = skeleton.GetJoint(jointEnum1).Position;
            Vector3 j2 = skeleton.GetJoint(jointEnum2).Position;
            Console.WriteLine("joint 1" + j1.ToString());
            Console.WriteLine("joint 2" + j2.ToString());
            return Vector3.Distance(skeleton.GetJoint(jointEnum1).Position, skeleton.GetJoint(jointEnum2).Position);
        }
        public Vector3 HeadPosition()
        {
            return Vector3.Divide(Vector3.Add(skeleton.GetJoint((int)JointIndices.EyeRight).Position, skeleton.GetJoint((int)JointIndices.EyeLeft).Position),2); 
            //Indices for vectors can be found at https://docs.microsoft.com/en-us/azure/kinect-dk/body-joints
        }
       
        public bool BecomeLeader() //A "t-pose" gesture is used to manually take control of the Kinect tracking
        {
            return RightArmLevelWithNeck() && LeftArmLevelWithNeck();
        }
        private bool RightArmLevelWithNeck()
        {
           var wristRight = skeleton.GetJoint((int)JointIndices.WristRight);
           var elbowRight = skeleton.GetJoint((int)JointIndices.ElbowRight);
           var shoulderRight = skeleton.GetJoint((int)JointIndices.ShoulderRight);
           var armJoints = new Joint[] { wristRight, elbowRight, shoulderRight };
            return JointsWithenTolerances(armJoints, ActionTriggerTolerance, false, true, true);

        }
        private bool LeftArmLevelWithNeck()
        {
            var wristLeft = skeleton.GetJoint((int)JointIndices.WristLeft);
            var elbowLeft = skeleton.GetJoint((int)JointIndices.ElbowLeft);
            var shoulderLeft = skeleton.GetJoint((int)JointIndices.ShoulderLeft);
            var armJoints = new Joint[] { wristLeft, elbowLeft, shoulderLeft };
            return JointsWithenTolerances(armJoints, ActionTriggerTolerance, false, true, true);

        }
       
        private bool JointsWithenTolerances(Joint[] joints, int tolerance, bool xAxis, bool yAxis, bool zAxis)
        {
            var jointPositions =  Array.ConvertAll(joints, new Converter<Joint, Vector3>(JointToVector));
            foreach(Vector3 vector3 in jointPositions)
            {
                foreach(Vector3 comparedVector3 in jointPositions)
                {
                    if (!WithenTolerance(vector3, comparedVector3, tolerance, xAxis, yAxis, zAxis))
                        return false;
                       
                }
            }
            return true;

        }
        private Vector3 JointToVector(Joint joint)
        {
            return joint.Position;
        }
     
        private bool WithenTolerance(Vector3 positionOne, Vector3 positionTwo, int tolerance, bool xAxis, bool yAxis, bool zAxis)
        {
            Vector3 result = Vector3.Abs(Vector3.Subtract(positionOne, positionTwo));
            return ((result.X < tolerance || !xAxis) && (result.Y < tolerance || !yAxis) && (result.Z < tolerance || !zAxis));
        }

        public event EventHandler<CancelThresholdEventArgs>  CancelThresholdReached;
    }
    public class CancelThresholdEventArgs : EventArgs
    {
        public int Threshold { get; set; }

    }
}
