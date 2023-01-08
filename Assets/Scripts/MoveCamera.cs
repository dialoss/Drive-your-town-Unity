using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{

    public float turnSpeed = 4.0f;      // Speed of camera turning when mouse moves in along an axis
    public float panSpeed = 4.0f;       // Speed of the camera when being panned
    public float zoomSpeed = 4.0f;      // Speed of the camera going back and forth

    private Vector3 mouseOrigin;    // Position of cursor when mouse dragging starts
    private bool isPanning = false;     // Is the camera being panned?
    private bool isRotating = false;    // Is the camera being rotated?
    private bool isZooming = false; // Is the camera zooming?

    //public float speed = 10f;
    //public float acceleration = 0.1f;

    //private float speedFactor = 0.1f;

    //public static float ClampAngle(float angle, float min, float max)
    //{
    //    if (angle < -360.0f)
    //        angle += 360.0f;
    //    if (angle > 360.0f)
    //        angle -= 360.0f;
    //    return Mathf.Clamp(angle, min, max);
    //}

    /*
    void Update()
    {
        float localSpeed = Time.deltaTime * speed * speedFactor;

        float xAxisMove = Input.GetAxis("Horizontal") * localSpeed;
        float zAxisVMove = Input.GetAxis("Vertical") * localSpeed;

        //float xAxisRot = Input.GetAxisRaw("Mouse X");
        //float zAxisRot = Input.GetAxisRaw("Mouse Y");

        float yValue = 0.0f;
        if (Input.GetKey(KeyCode.Q))
            yValue = -localSpeed;
        else if (Input.GetKey(KeyCode.E))
            yValue = localSpeed;

        if (xAxisMove != 0.0f || zAxisVMove != 0.0f || yValue != 0.0f)
            speedFactor += acceleration;
        else
            speedFactor -= acceleration * 4;

        speedFactor = Mathf.Clamp(speedFactor, 0.1f, 10.0f);

        Camera mycam = GetComponent<Camera>();

        float sensitivity = 0.05f;
        Vector3 vp = mycam.ScreenToViewportPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mycam.nearClipPlane));
        vp.x -= 0.5f;
        vp.y -= 0.5f;
        vp.x *= sensitivity;
        vp.y *= sensitivity;
        vp.x += 0.5f;
        vp.y += 0.5f;
        Vector3 sp = mycam.ViewportToScreenPoint(vp);

        Vector3 v = mycam.ScreenToWorldPoint(sp);
        transform.LookAt(v, Vector3.up);

        //zAxisRot = ClampAngle(zAxisRot, -360, 360);
        Quaternion rotation = Quaternion.Euler(v.x, v.y, v.z);
        //Vector3 position += Vector3(transform.position.x + xAxisMove, transform.position.y + yValue, transform.position.z + zAxisVMove);
        transform.position += rotation * new Vector3(xAxisMove, yValue, zAxisVMove);
        //transform.rotation = rotation;
    }
    */

    void checkRotating()
    {
        //if (Input.GetAxisRaw("Mouse X") != 0 ||
        //    Input.GetAxisRaw("Mouse Y") != 0)
        if (Input.GetMouseButtonDown(0))
        {
            // Get mouse origin
            mouseOrigin = Input.mousePosition;
            isRotating = true;
        }
        else
            isRotating = false;
    }

    bool checkPanning()
    {
        if (Input.GetMouseButtonDown(1) ||
            Input.GetAxis("Horizontal") != 0 ||
            Input.GetAxis("Vertical") != 0)
        {
            Debug.Log("panning!");
            return true;
        }
        
        return false;
    }

    void checkZooming()
    {
        if (Input.GetMouseButton(2))
        {
            // Get mouse origin
            mouseOrigin = Input.mousePosition;
            isZooming = true;
        }
        else
            isZooming = false;
    }

    void Update()
    {
        Camera cam = GetComponent<Camera>();

        // Get the left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            // Get mouse origin
            mouseOrigin = Input.mousePosition;
            isRotating = true;
        }

        //checkPanning();
        //checkRotating();
        //checkZooming();


        //// Get the right mouse button
        if (checkPanning())
        {
            // Get mouse origin
            mouseOrigin = Input.mousePosition;
            isPanning = true;
        }
        //if (checkPanning())
        //{
        //    // Get mouse origin
        //    mouseOrigin = Input.mousePosition;
        //    isPanning = true;
        //}

        //// Get the middle mouse button
        if (Input.GetMouseButtonDown(2))
        {
            // Get mouse origin
            mouseOrigin = Input.mousePosition;
            isZooming = true;
        }

        //// Disable movements on button release
        if (!Input.GetMouseButton(0)) isRotating = false;
        if (!Input.GetMouseButton(1)) isPanning = false;
        if (!Input.GetMouseButton(2)) isZooming = false;

        // Rotate camera along X and Y axis
        if (isRotating)
        {
            Vector3 pos = cam.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);

            transform.RotateAround(transform.position, transform.right, -pos.y * turnSpeed);
            transform.RotateAround(transform.position, Vector3.up, pos.x * turnSpeed);
        }

        // Move the camera on it's XY plane
        if (isPanning)
        {
            Vector3 pos = cam.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);

            Vector3 move = new Vector3(pos.x * panSpeed, pos.y * panSpeed, 0);
            transform.Translate(move, Space.Self);
        }

        // Move the camera linearly along Z axis
        if (isZooming)
        {
            Vector3 pos = cam.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);

            Vector3 move = pos.y * zoomSpeed * transform.forward;
            transform.Translate(move, Space.World);
        }
    }
}
