   M S B A M L   `   `   `  ����^  ZMicrosoft.Samples.Kinect.WpfViewers, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null�^clr-namespace:Microsoft.Samples.Kinect.WpfViewers;assembly=Microsoft.Samples.Kinect.WpfViewers#Microsoft.Samples.Kinect.WpfViewers  5   	    	ShapeGame   ShapeGame.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   kkt^clr-namespace:Microsoft.Samples.Kinect.WpfViewers;assembly=Microsoft.Samples.Kinect.WpfViewers    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   �   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     6�   -   5        $��
mainWindow��5   q   $	��600��5   	   $	��800��5   	     � Title$  Kinect3D��5   	   $��Italic$�6   .��5      � $	��800��6   $	��600��6   .+�5	   
   =   7Microsoft.Samples.Kinect.WpfViewers.KinectSensorChooser  -   5          Name$ SensorChooser��5	   n   $��223,0,0,133q�6!   $��Center=�66   $��Center=�6S   $��66��6�   $	��124��6�   '� 5
   
   -   5        $��myViewport3D��5
      6+   �� 5   
   -   5         ��Name$ distanceTextBlock��5      $��25(�6/   $��
Distance: ��6=   $	��Top=�6O   $
��Left=�6g   $��
-80,10,0,0q�6�   �� 5   
   -   5         ��Name$ distanceTextBox��5      $��25(�6+   $��
Distance: ��69   $	��Top=�6K   $
��Left=�6c   $��
120,10,0,0q�6~   �� 5   
   -   5        $ CheckHandAcceleration��5      $��25(�61   $��Action: ��6?   $	��Top=�6O   $
��Left=�6g   $��
120,50,0,0q�6�   �� 5   
   -   5         ��Name$ kinectImage��5      $
��Fill=�6%   $	��Top=�64   $��Right=�6L   $��	0,0,-80,0q�6h   5      5      