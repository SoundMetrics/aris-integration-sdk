// Copyright (c) 2017-2019 Sound Metrics. All Rights Reserved.

using System;
using System.Collections.Generic;
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
                RebuildTransform(XRotationProperty);
                RebuildTransform(YRotationProperty);
                RebuildTransform(ZRotationProperty);
            }
        }

        // The default inverts are set up to work correctly with
        // compass values.
        //      X axis <- CompassPitch
        //      Y axis <- CompassHeading
        //      Z axis <- CompassRolls
        private const bool DefaultXInvert = false;
        private const bool DefaultYInvert = true;
        private const bool DefaultZInvert = true;

        public static readonly DependencyProperty XRotationProperty =
            DependencyProperty.Register(
                nameof(XRotation),
                typeof(double),
                typeof(OrientationControl),
                new PropertyMetadata(0.0, OnAxisChanged));
        public static readonly DependencyProperty XInvertProperty =
            DependencyProperty.Register(
                nameof(XInvert),
                typeof(bool),
                typeof(OrientationControl),
                new PropertyMetadata(DefaultXInvert, OnInvertChanged));
        public static readonly DependencyProperty YRotationProperty =
            DependencyProperty.Register(
                nameof(YRotation),
                typeof(double),
                typeof(OrientationControl),
                new PropertyMetadata(0.0, OnAxisChanged));
        public static readonly DependencyProperty YInvertProperty =
            DependencyProperty.Register(
                nameof(YInvert),
                typeof(bool),
                typeof(OrientationControl),
                new PropertyMetadata(DefaultYInvert, OnInvertChanged));
        public static readonly DependencyProperty ZRotationProperty =
            DependencyProperty.Register(
                nameof(ZRotation),
                typeof(double),
                typeof(OrientationControl),
                new PropertyMetadata(0.0, OnAxisChanged));
        public static readonly DependencyProperty ZInvertProperty =
            DependencyProperty.Register(
                nameof(ZInvert),
                typeof(bool),
                typeof(OrientationControl),
                new PropertyMetadata(DefaultZInvert, OnInvertChanged));

        /// <summary>
        /// Generally bound to header.CompassPitch.
        /// </summary>
        public double XRotation
        {
            get { return (double)GetValue(XRotationProperty); }
            set { ValidateAndSet(XRotationProperty, value); }
        }

        /// <summary>
        /// Inverts the sign of the X axis.
        /// Default is False, which is appropriate for working with
        /// header.CompassPitch.
        /// </summary>
        public bool XInvert
        {
            get => (bool)GetValue(XInvertProperty);
            set { SetValue(XInvertProperty, value); }
        }

        /// <summary>
        /// Generally bound to header.CompassHeading.
        /// </summary>
        public double YRotation
        {
            get { return (double)GetValue(YRotationProperty); }
            set { ValidateAndSet(YRotationProperty, value); }
        }

        /// <summary>
        /// Inverts the sign of the Y axis.
        /// Default is True, which is appropriate for working with
        /// header.CompassHeading.
        /// </summary>
        public bool YInvert
        {
            get => (bool)GetValue(YInvertProperty);
            set { SetValue(YInvertProperty, value); }
        }

        /// <summary>
        /// Generally bound to header.CompassRoll.
        /// Default is True, which is appropriate for working with
        /// header.CompassRoll.
        /// </summary>
        public double ZRotation
        {
            get { return (double)GetValue(ZRotationProperty); }
            set { ValidateAndSet(ZRotationProperty, value); }
        }

        /// <summary>
        /// Inverts the sign of the Z axis.
        /// </summary>
        public bool ZInvert
        {
            get => (bool)GetValue(ZInvertProperty);
            set { SetValue(ZInvertProperty, value); }
        }

        private static void OnAxisChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var ctl = (OrientationControl)d;
            ctl.RebuildTransform(e.Property);
        }

        private void RebuildTransform(DependencyProperty axisProperty)
        {
            ValueTuple<RotateTransform3D, double, bool> inputs;

            if (axisProperty == XRotationProperty)
            {
                inputs = (_xRotation, XRotation, XInvert);
            }
            else if (axisProperty == YRotationProperty)
            {
                inputs = (_yRotation, YRotation, YInvert);
            }
            else if (axisProperty == ZRotationProperty)
            {
                inputs = (_zRotation, ZRotation, ZInvert);
            }
            else
            {
                Debug.Assert(false, "OrientationControl.RebuildTransform: Unhandled dependency property");
                return;
            }

            var (rotation, value, invert) = inputs;
            if (rotation != null)
            {
                rotation.Rotation = BuildNewRotation(rotation, value, invert);
            }
        }

        private static void OnInvertChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var ctl = (OrientationControl)d;
            ctl.RebuildTransform(e.Property);
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

            public static AxisAngleRotation3D BuildNewRotation(
                RotateTransform3D txfm,
                double angle,
                bool invert)
            {
                Debug.Assert(txfm.Rotation.GetType() == typeof(AxisAngleRotation3D));
                var rot = (AxisAngleRotation3D)txfm.Rotation;
                var nInvert = invert ? -1 : 1;
                return new AxisAngleRotation3D(rot.Axis, nInvert * angle);
            }
        }
    }
}
