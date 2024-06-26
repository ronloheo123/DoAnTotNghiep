//------------------------------------------------------------------------------
// <copyright file="Player.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace ShapeGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Kinect;
    using System.Windows.Media.Media3D;
    using WindowsInput;
    using WindowsInput.Native;
    public class Player
    {

        JointCollection joints;
        public double TorsoToGroundDistance { get; private set; }


        // Keeping track of all bone segments of interest as well as head, hands and feet
        //private readonly Dictionary<Bone, BoneData> segments = new Dictionary<Bone, BoneData>();
        private readonly int id;
        private Rect playerBounds;
        private System.Windows.Point playerCenter;
        private double playerScale;
        private Cube head, leftArm, rightArm, rightLeg, leftLeg, rightCalf, leftCalf, rightForearm, leftForearm, torso, chest;
        private Cube leftHand, rightHand, leftFoot, rightFoot;
        private bool isHandRaised = false;
        private DateTime handRaisedTime;
        private int activationTimeInSeconds = 3;
        public bool active = false;
        private double distanceToCamera;
        private double distanceToCamerahand;

        // Khai báo biến lưu trữ vị trí trước đó của bàn tay phải
        private Point3D previousHandPosition;

        // Hằng số đại diện cho ngưỡng gia tốc để xem xét là một sự kiện gia tốc hay không
        private const double AccelerationThreshold = 0.15; // Giá trị này có thể điều chỉnh tùy theo nhu cầu
        private DateTime lastNotificationTime = DateTime.MinValue;
        private bool isAccelerating = false; // Biến để xác định xem có đạt đủ ngưỡng gia tốc hay không
        private DateTime accelerationStartTime; // Thời điểm bắt đầu gia tốc

        public Player(int skeletonSlot, Viewport3D myViewport3D)
        {
            this.id = skeletonSlot;
            this.head = new Cube(myViewport3D, (float)0.5);
            this.leftArm = new Cube(myViewport3D, (float)0.2);
            this.rightArm = new Cube(myViewport3D, (float)0.2);
            this.rightLeg = new Cube(myViewport3D, (float)0.2);
            this.leftLeg = new Cube(myViewport3D, (float)0.2);
            this.torso = new Cube(myViewport3D, (float)0.6); // lấy vị trí này
            this.chest = new Cube(myViewport3D, (float)0.45);
            this.leftForearm = new Cube(myViewport3D, (float)0.2);
            this.rightForearm = new Cube(myViewport3D, (float)0.2);
            this.leftCalf = new Cube(myViewport3D, (float)0.2);
            this.rightCalf = new Cube(myViewport3D, (float)0.2);
            this.leftHand = new Cube(myViewport3D, (float)0.7);
            this.rightHand = new Cube(myViewport3D, (float)0.7);
            this.leftFoot = new Cube(myViewport3D, (float)0.7);
            this.rightFoot = new Cube(myViewport3D, (float)0.7);
            this.LastUpdated = DateTime.Now;
            //setupMesh(myViewport3D);   
        }



        public bool IsAlive { get; set; }

        public DateTime LastUpdated { get; set; }

        public int GetId()
        {
            return this.id;
        }

        public void SetBounds(Rect r)
        {
            this.playerBounds = r;
            this.playerCenter.X = (this.playerBounds.Left + this.playerBounds.Right) / 2;
            this.playerCenter.Y = (this.playerBounds.Top + this.playerBounds.Bottom) / 2;
            this.playerScale = Math.Min(this.playerBounds.Width, this.playerBounds.Height / 2);
        }

        public void UpdateAllJoints(Microsoft.Kinect.JointCollection joints)
        {
            this.joints = joints;
        }













        private void CheckHandRaised()
        {
            if (this.joints[JointType.HandRight].Position.Y > this.joints[JointType.Head].Position.Y &&
                this.joints[JointType.HandLeft].Position.Y > this.joints[JointType.Head].Position.Y)
            {
                // Nếu tay được giơ lên và chưa active
                if (active == false)
                {
                    // Nếu chưa ghi nhận thời điểm tay được giơ lên lần đầu, ghi nhận thời điểm hiện tại
                    if (handRaisedTime == DateTime.MinValue)
                    {
                        handRaisedTime = DateTime.Now;
                    }
                    else
                    {
                        // Nếu đã ghi nhận thời điểm tay được giơ lên lần đầu, kiểm tra thời gian giữa thời điểm đó và thời điểm hiện tại
                        if ((DateTime.Now - handRaisedTime).TotalSeconds >= activationTimeInSeconds)
                        {
                            // Kiểm tra khoảng cách từ người chơi đến camera
                            DistanceFromBodyToCamera();
                            // Nếu trong khoảng cách mong muốn, đặt trạng thái active thành true
                            if (distanceToCamera >= 1.8 && distanceToCamera <= 2.0)
                            {
                                active = true;
                                handRaisedTime = DateTime.MinValue; // Reset thời điểm khi active được đặt thành true
                            }
                        }
                    }
                }
                if (active == true)
                {
                    // Nếu chưa ghi nhận thời điểm tay được giơ lên lần đầu, ghi nhận thời điểm hiện tại
                    if (handRaisedTime == DateTime.MinValue)
                    {
                        handRaisedTime = DateTime.Now;
                    }
                    else
                    {
                        // Nếu đã ghi nhận thời điểm tay được giơ lên lần đầu, kiểm tra thời gian giữa thời điểm đó và thời điểm hiện tại
                        if ((DateTime.Now - handRaisedTime).TotalSeconds >= activationTimeInSeconds)
                        {
                            // Kiểm tra khoảng cách từ người chơi đến camera
                            DistanceFromBodyToCamera();
                            // Nếu trong khoảng cách mong muốn, đặt trạng thái active thành true
                            if (distanceToCamera >= 1.8 && distanceToCamera <= 2.0)
                            {
                                active = false;
                                handRaisedTime = DateTime.MinValue; // Reset thời điểm khi active được đặt thành true
                            }
                        }
                    }
                }
            }
            else
            {
                // Nếu tay không được giơ lên, đặt lại thời điểm tay được giơ lên lần đầu tiên về giá trị mặc định
                handRaisedTime = DateTime.MinValue;
            }
        }





        private void distanceandcheck()
        {
            bool hasValidJoints = joints.Any(joint => joint.TrackingState == JointTrackingState.Tracked);

            if (hasValidJoints)
            {
                // Nếu có ít nhất một joint được theo dõi, tính khoảng cách từ torso đến mặt đất
                double minFootHeight = Math.Min(joints[JointType.FootLeft].Position.Y, joints[JointType.FootRight].Position.Y);
                TorsoToGroundDistance = Math.Abs(joints[JointType.HipCenter].Position.Y - minFootHeight);

                // Xử lý nếu khoảng cách từ torso đến mặt đất < 0.4m thì người chơi đang ngồi, ngược lại đang đứng
                bool isSitting = TorsoToGroundDistance < 0.4;

                // Cập nhật trực tiếp trên màn hình thông qua MainWindow
                if (MainWindow.Instance != null)
                {
                    // Hiển thị trạng thái ngồi hoặc đứng trên màn hình
                    string status = isSitting ? "Người chơi đang ngồi." : "Người chơi đang đứng.";
                    MainWindow.Instance.UpdateDistanceText(status, TorsoToGroundDistance);
                }
            }
            else
            {
                // Nếu không có joint nào được theo dõi, đặt giá trị khoảng cách là 0 và trạng thái là "Không có người"
                TorsoToGroundDistance = 0;
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.UpdateDistanceText("Không có người", TorsoToGroundDistance);
                }
            }
        }
        private double CalculateDistance(Point3D point1, Point3D point2)
        {
            // Tính toán khoảng cách giữa hai điểm trong không gian 3D
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            double dz = point2.Z - point1.Z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }


        /*  private void DitanceFromCameratoRightHand()
          {
             Point3D rightHandPosition = new Point3D(
                   joints[JointType.HandRight].Position.X,
                   joints[JointType.HandRight].Position.Y,
                   joints[JointType.HandRight].Position.Z);
             Point3D chestPosition = new Point3D(
                   joints[JointType.ShoulderCenter].Position.X,
                   joints[JointType.ShoulderCenter].Position.Y,
                   joints[JointType.ShoulderCenter].Position.Z);
             Point3D cameraCenter = new Point3D(0, 0, 0);
             double distance = CalculateDistance(rightHandPosition, chestPosition); //40->60
              //distanceToCamerahand = CalculateDistance(rightHandPosition, cameraCenter);
             MainWindow.Instance.UpdateDistanceTextFromBodytoHand(distance);

          }
        */
        private void DistanceFromBodyToCamera()
        {
            // Lấy vị trí của điểm đầu của người từ dữ liệu cảm biến Kinect
            Point3D headPosition = new Point3D(
                joints[JointType.Head].Position.X,
                joints[JointType.Head].Position.Y,
                joints[JointType.Head].Position.Z);


            // Giả sử vị trí tâm của camera là (0, 0, 0) trong không gian 3D
            Point3D cameraCenter = new Point3D(0, 0, 0);

            // Tính toán khoảng cách từ điểm đầu của người đến tâm của camera
            distanceToCamera = CalculateDistance(headPosition, cameraCenter);
            MainWindow.Instance.UpdateDistanceTextFromBodyToCamera(distanceToCamera);
        }
        private void DetectAcceleration(Point3D currentHandPosition)
        {
            if (previousHandPosition != null)
            {
                // Tính toán khoảng cách di chuyển theo các trục
                double displacementX = Math.Abs(currentHandPosition.X - previousHandPosition.X);
                double displacementY = Math.Abs(currentHandPosition.Y - previousHandPosition.Y);
                double displacementZ = Math.Abs(currentHandPosition.Z - previousHandPosition.Z);

                // Kiểm tra xem khoảng cách di chuyển theo các trục có vượt quá ngưỡng gia tốc không
                if (displacementX > AccelerationThreshold || displacementY > AccelerationThreshold || displacementZ > AccelerationThreshold)
                {
                    VirtualKeyCode keyCode;
                    string accelerationDirection = "None";

                    // Chọn hướng di chuyển theo ưu tiên: trục Y, sau đó trục X, cuối cùng là trục Z
                    if (displacementY > displacementX && displacementY > displacementZ)
                    {
                        // Di chuyển theo trục Y
                        keyCode = currentHandPosition.Y > previousHandPosition.Y ? VirtualKeyCode.UP : VirtualKeyCode.DOWN;
                    }
                    else if (displacementX > displacementZ)
                    {
                        // Di chuyển theo trục X
                        keyCode = currentHandPosition.X > previousHandPosition.X ? VirtualKeyCode.RIGHT : VirtualKeyCode.LEFT;
                    }
                    else
                    {
                        // Di chuyển theo trục Z
                        keyCode = currentHandPosition.Z > previousHandPosition.Z ? VirtualKeyCode.ESCAPE : VirtualKeyCode.RETURN;
                    }

                    // Mô phỏng phím được nhấn
                    InputSimulator simulator = new InputSimulator();
                    simulator.Keyboard.KeyPress(keyCode);

                    // Ghi nhận rằng đang có gia tốc và lưu thời điểm bắt đầu gia tốc
                    isAccelerating = true;
                    accelerationStartTime = DateTime.Now;

                    // Gửi hướng di chuyển đến giao diện người dùng (nếu cần)
                    MainWindow.Instance.CheckHandAccelerationText(accelerationDirection);
                }
                else
                {
                    // Kiểm tra xem có đang trong trạng thái gia tốc không
                    if (isAccelerating)
                    {
                        // Kiểm tra xem đã đủ thời gian để chuyển sang "None" chưa
                        if ((DateTime.Now - accelerationStartTime).TotalMilliseconds > 1000) // Chuyển sang "None" sau 1 giây
                        {
                            string direction = "None";
                            MainWindow.Instance.CheckHandAccelerationText(direction);

                            // Đặt lại trạng thái gia tốc
                            isAccelerating = false;
                        }
                    }
                }

                // Cập nhật vị trí trước đó là vị trí hiện tại để sử dụng trong lần kiểm tra tiếp theo
                previousHandPosition = currentHandPosition;
            }
            else
            {
                // Nếu đây là lần đầu tiên kiểm tra, chỉ cập nhật vị trí trước đó
                previousHandPosition = currentHandPosition;
            }
        }



        private void StatusAction()
        {
            string status = active ? "TRUE" : "FALSE";
            MainWindow.Instance.StatusActive(status);
        }

        public void Draw()
        {
            if (!this.IsAlive)
            {
                return;
            }
            // Lấy màu xanh lá
            Color greenColor = Colors.Green;

            // Draw all bones first, then circles (head and hands).
            DateTime cur = DateTime.Now;

            Point3DCollection myPositionCollection = new Point3DCollection();

            Console.WriteLine("{0} {1} {2}",
                    (float)joints[JointType.Head].Position.X, (float)joints[JointType.Head].Position.Y, (float)joints[JointType.Head].Position.Z
            );

            this.head.update(joints[JointType.Head].Position, joints[JointType.ShoulderCenter].Position);

            this.leftArm.update(joints[JointType.ShoulderLeft].Position, joints[JointType.ElbowLeft].Position);
            this.rightArm.update(joints[JointType.ShoulderRight].Position, joints[JointType.ElbowRight].Position);
            this.leftForearm.update(joints[JointType.ElbowLeft].Position, joints[JointType.WristLeft].Position);
            this.rightForearm.update(joints[JointType.ElbowRight].Position, joints[JointType.WristRight].Position);
            this.leftHand.update(joints[JointType.WristLeft].Position, joints[JointType.HandLeft].Position);
            this.rightHand.update(joints[JointType.WristRight].Position, joints[JointType.HandRight].Position);

            this.leftLeg.update(joints[JointType.HipLeft].Position, joints[JointType.KneeLeft].Position);
            this.rightLeg.update(joints[JointType.HipRight].Position, joints[JointType.KneeRight].Position);
            this.leftCalf.update(joints[JointType.KneeLeft].Position, joints[JointType.AnkleLeft].Position);
            this.rightCalf.update(joints[JointType.KneeRight].Position, joints[JointType.AnkleRight].Position);
            this.leftFoot.update(joints[JointType.AnkleLeft].Position, joints[JointType.FootLeft].Position);
            this.rightFoot.update(joints[JointType.AnkleRight].Position, joints[JointType.FootRight].Position);
            this.chest.update(joints[JointType.ShoulderCenter].Position, joints[JointType.Spine].Position);
            this.torso.update(joints[JointType.Spine].Position, joints[JointType.HipCenter].Position);
            DistanceFromBodyToCamera();
            CheckHandRaised();
            StatusAction();
            if (active == true)
            {

                distanceandcheck();

                Point3D currentHandPosition = new Point3D(joints[JointType.HandRight].Position.X,
                                             joints[JointType.HandRight].Position.Y,
                                             joints[JointType.HandRight].Position.Z);
                DetectAcceleration(currentHandPosition);

            }


        }
    }
}
