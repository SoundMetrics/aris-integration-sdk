// Copyright (c) 2017-2019 Sound Metrics. All Rights Reserved.

using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace SoundMetrics.DataVisualization
{
    using static OrientationControl.VisualHelpers;

    /// <summary>
    /// Interaction logic for OrientationControl.xaml
    /// </summary>
    public partial class OrientationControl : UserControl
    {
        public OrientationControl()
        {
            InitializeComponent();
            Loaded += (s, e) => {
                Initialize3DView();

                // The initial values get loaded before the control is done loading,
                // which causes the image not to be updated, so force the values once loaded.
                ForceValueUpdates();
            };

            void ForceValueUpdates()
            {
                _xRotation.Rotation = BuildNewRotation(_xRotation, XRotation);
                _yRotation.Rotation = BuildNewRotation(_yRotation, YRotation);
                _zRotation.Rotation = BuildNewRotation(_zRotation, ZRotation);
            }
        }

        public static readonly DependencyProperty XRotationProperty =
            DependencyProperty.Register(
                "XRotation",
                typeof(double),
                typeof(OrientationControl),
                new PropertyMetadata(0.0, OnAxisChanged));
        public static readonly DependencyProperty YRotationProperty =
            DependencyProperty.Register(
                "YRotation",
                typeof(double),
                typeof(OrientationControl),
                new PropertyMetadata(0.0, OnAxisChanged));
        public static readonly DependencyProperty ZRotationProperty =
            DependencyProperty.Register(
                "ZRotation",
                typeof(double),
                typeof(OrientationControl),
                new PropertyMetadata(0.0, OnAxisChanged));

        public double XRotation
        {
            get { return (double)GetValue(XRotationProperty); }
            set { ValidateAndSet(XRotationProperty, value); }
        }

        public double YRotation
        {
            get { return (double)GetValue(YRotationProperty); }
            set { ValidateAndSet(YRotationProperty, value); }
        }

        public double ZRotation
        {
            get { return (double)GetValue(ZRotationProperty); }
            set { ValidateAndSet(ZRotationProperty, value); }
        }

        private static void OnAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (OrientationControl)d;

            if (e.Property == XRotationProperty)
            {
                if (ctl._xRotation != null)
                {
                    ctl._xRotation.Rotation = BuildNewRotation(ctl._xRotation, ctl.XRotation);
                }
            }
            else if (e.Property == YRotationProperty)
            {
                if (ctl._yRotation != null)
                {
                    ctl._yRotation.Rotation = BuildNewRotation(ctl._yRotation, ctl.YRotation);
                }
            }
            else if (e.Property == ZRotationProperty)
            {
                if (ctl._zRotation != null)
                {
                    ctl._zRotation.Rotation = BuildNewRotation(ctl._zRotation, ctl.ZRotation);
                }
            }
            else
            {
                Debug.Assert(false, "Unhandled dependency property");
            }
        }

        private void Initialize3DView()
        {
            _viewport = GetViewport();
            Debug.Assert(_viewport != null);

            _viewport.Camera = CreateCamera();
            CreateTransformElements(_viewport, out _xRotation, out _yRotation, out _zRotation);

            Debug.Assert(_xRotation != null);
            Debug.Assert(_yRotation != null);
            Debug.Assert(_zRotation != null);
        }

        private Viewport3D GetViewport()
        {
            // Children isn't a typed collection, so 'object' type...
            var viewport = (from object x in viewContainer.Children
                            where (x is Viewport3D)
                            select x).Cast<Viewport3D>().SingleOrDefault();
            Debug.Assert(viewport != null);
            return viewport;
        }

        internal static (bool success, double value) ValidateRotation(double rotation)
        {
            if (double.IsNaN(rotation) || double.IsInfinity(rotation))
            {
                return (false, 0.0);
            }

            return (true, rotation);
        }

        // NOTE: This method is called only from the *Rotation properties. This occurs only
        // when the properties are set directly; bindings to the properties do not set the
        // properties directly, they call DependencyObject.SetValue(), bypassing the
        // properties. Therefore, this property-use-only method reverses the rotation as
        // does the RotatorValueConvert type, which is used only with bindings.
        private void ValidateAndSet(DependencyProperty dp, double rotation)
        {
            (bool success, double validated) = ValidateRotation(rotation);

            if (success)
            {
                // Sign of rotation in WPF 3D doesn't match sign of rotation of the Y & Z axes.
                if (dp == YRotationProperty || dp == ZRotationProperty)
                {
                    validated = -validated;
                }
            }

            SetValue(dp, validated);
        }

        private Viewport3D _viewport;
        private RotateTransform3D _xRotation, _yRotation, _zRotation;

        /// These methods help initialize the visual tree so that we can transform the
        /// 3D model on the fly. These are a convenience so that, when the model changes,
        /// we don't need to make updates to the model itself to make the code work.
        public static class VisualHelpers
        {
            /// <summary>
            /// Creates the camera used to view the model.
            /// </summary>
            public static OrthographicCamera CreateCamera()
            {
                var position = new Point3D(0.0, 0.0, 4.0);
                var lookDirection = new Vector3D(0.0, 0.0, -1.0); // Directly down the Z axis toward -Z
                var upDirection = new Vector3D(0.0, 1.0, 0.0); // +Y is up
                var width = 1.25;
                var nearPlaneDistance = -2.0;
                var farPlaneDistance = 5.0;

                return
                    new OrthographicCamera(position, lookDirection, upDirection, width)
                    {
                        NearPlaneDistance = nearPlaneDistance,
                        FarPlaneDistance = farPlaneDistance,
                    };
            }

            /// <summary>
            /// This function replaces the transform group of the original XAML
            /// with transforms that we can manually control.
            /// </summary>
            public static void CreateTransformElements(
                Viewport3D viewport, out RotateTransform3D xRotation,
                out RotateTransform3D yRotation, out RotateTransform3D zRotation)
            {
                Debug.Assert(viewport.Children.Count == 1);
                Debug.Assert(viewport.Children[0].GetType() == typeof(ModelVisual3D));
                var model = (ModelVisual3D)viewport.Children[0];

                xRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0));
                yRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0));
                zRotation = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0));

                // NOTE: The Z-axis transform must be first in  order, then X,
                // for things to transform correctly. So, Z, X, Y.
                var xfmGroup = new Transform3DGroup();
                xfmGroup.Children.Add(zRotation);
                xfmGroup.Children.Add(xRotation);
                xfmGroup.Children.Add(yRotation);

                model.Transform = xfmGroup;
            }

            public static AxisAngleRotation3D BuildNewRotation(RotateTransform3D txfm, double angle)
            {
                Debug.Assert(txfm.Rotation.GetType() == typeof(AxisAngleRotation3D));
                var rot = (AxisAngleRotation3D)txfm.Rotation;
                return new AxisAngleRotation3D(rot.Axis, angle);
            }
        }
    }
}
