// Unity SDK for Qualisys ack Manager. Copyright 2015-2023 Qualisys AB
//
using UnityEngine;

namespace QualisysRealTime.Unity
{
    static class MatrixExtensions
    {
        public static Vector3 ExtractPosition(this Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = matrix.m03;
            position.y = matrix.m13;
            position.z = matrix.m23;
            return position;
        }
    }


    class RTForcePlate : MonoBehaviour
    {
        public string forcePlateName = "Force-plate 1";

        public LineRenderer forceArrow;
        
        public LineRenderer momentArrow;
        
        public GameObject forcePlateCube;

        private ForceVector forceVectorCached;

        Vector3 VisualDownscaleForce(Vector3 v)
        { 
            // Downscale to look good in the scene
            // Inverted to display above the force plate
            return v / -500.0f;
        }

        Vector3 VisualDownscaleMoment(Vector3 v)
        { 
            // Downscale to look good in the scene
            // Inverted to be compatible with the force
            return v / -100.0f;
        }

        void UpdateArrow( LineRenderer lineRenderer, Vector3 position, Vector3 directionAndMagnitude )
        { 
            Vector3 endPosition = position + directionAndMagnitude;
            Vector3 startPosition = position;
                    
            float headLength = 0.15f;
            float headWidth = 0.1f;
            float stemWidth = headWidth / 4.0f;

            float minLength = headLength;
            float length = Vector3.Distance (startPosition,  endPosition);
                    
            lineRenderer.enabled = length >= minLength;
                    
            if(lineRenderer.enabled)
            {   
                //   .   _1.0
                //  / \
                // /. .\ _breakpoint
                //  | |  
                //  |_|  _0.0

                float breakpoint = headLength / length;
                        
                //Making an arrow using the line renderer.
                //Code adapted from an answer at the Unity Forum.
                //ShawnFeatherly (http://answers.unity.com/answers/1330338/view.html)
                lineRenderer.positionCount = 4;
                lineRenderer.SetPosition (0, startPosition);
                lineRenderer.SetPosition (1, Vector3.Lerp(startPosition,  endPosition, 0.999f - breakpoint));
                lineRenderer.SetPosition (2, Vector3.Lerp (startPosition,  endPosition, 1 - breakpoint));
                lineRenderer.SetPosition (3,  endPosition);
                lineRenderer.useWorldSpace = false;
                lineRenderer.widthCurve = new AnimationCurve (
                        new Keyframe (0, stemWidth),
                        new Keyframe (0.999f - breakpoint, stemWidth),
                        new Keyframe (1 - breakpoint, headWidth),
                        new Keyframe (1, 0f));
            }
        }

        void Update()
        {
            forceVectorCached = RTClient.GetInstance().GetForceVector(forcePlateName);
            
            if (forcePlateCube) 
            {
                forcePlateCube.SetActive(forceVectorCached != null);
                if (forceVectorCached != null)
                {
                    // Adjust cube to fit force plate
                    var src = forceVectorCached.Transform;
                    var forcePlateThickness = 0.02f;
                    var destTransform = forcePlateCube.transform;

                    destTransform.localRotation = src.rotation;
                    destTransform.localScale = new Vector3(
                        Vector3.Distance(forceVectorCached.Corners[0], forceVectorCached.Corners[1]),
                        Vector3.Distance(forceVectorCached.Corners[1], forceVectorCached.Corners[2]),
                        forcePlateThickness
                    );

                    destTransform.position = transform.TransformVector(src.ExtractPosition()) - destTransform.forward * (forcePlateThickness / 2.0f) + this.transform.position;
                }
            }
            
            if (forceArrow) 
            {
                forceArrow.gameObject.SetActive(forceVectorCached != null);
                if (forceVectorCached != null)
                {
                    UpdateArrow(forceArrow, forceVectorCached.ApplicationPoint, VisualDownscaleForce(forceVectorCached.Force));
                }
            }

            if (momentArrow) 
            {
                momentArrow.gameObject.SetActive(forceVectorCached != null);
                if (forceVectorCached != null)
                {
                    UpdateArrow(momentArrow, forceVectorCached.ApplicationPoint, VisualDownscaleMoment(forceVectorCached.Moment));
                }
            }
        }

        private void OnDrawGizmos()
        {
            if(forceVectorCached != null)
            {

                Vector3 zero = transform.TransformDirection(forceVectorCached.Transform.MultiplyPoint(Vector3.zero));
                Vector3 right = transform.TransformDirection(forceVectorCached.Transform.MultiplyPoint(Vector3.right));
                Vector3 up = transform.TransformDirection(forceVectorCached.Transform.MultiplyPoint(Vector3.up));
                Vector3 forward = transform.TransformDirection(forceVectorCached.Transform.MultiplyPoint(Vector3.forward));

                Gizmos.color = Color.green;
                Gizmos.DrawLine(zero + this.transform.position, up + this.transform.position);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(zero + this.transform.position, right + this.transform.position);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(zero + this.transform.position, forward + this.transform.position);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.TransformVector(forceVectorCached.ApplicationPoint) + this.transform.position, transform.TransformVector(forceVectorCached.ApplicationPoint) + transform.TransformVector(VisualDownscaleForce(forceVectorCached.Force)) + this.transform.position);
                Gizmos.DrawSphere(transform.TransformVector(forceVectorCached.ApplicationPoint) + this.transform.position, 0.01f);
                
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.TransformVector(forceVectorCached.Corners[0]) + this.transform.position, transform.TransformVector(forceVectorCached.Corners[1]) + this.transform.position);
                Gizmos.DrawLine(transform.TransformVector(forceVectorCached.Corners[1]) + this.transform.position,transform.TransformVector(forceVectorCached.Corners[2]) + this.transform.position);
                Gizmos.DrawLine(transform.TransformVector(forceVectorCached.Corners[2]) + this.transform.position,transform.TransformVector(forceVectorCached.Corners[3]) + this.transform.position);
                Gizmos.DrawLine(transform.TransformVector(forceVectorCached.Corners[3]) + this.transform.position, transform.TransformVector(forceVectorCached.Corners[0]) + this.transform.position);

                int i = 1;
                foreach( var corner in forceVectorCached.Corners )
                { 
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(transform.TransformVector(corner) + this.transform.position, (i++).ToString() );
                    #endif
                    Gizmos.DrawSphere(transform.TransformVector(corner) + this.transform.position, 0.01f );
                }

            }
        }
    }
}
