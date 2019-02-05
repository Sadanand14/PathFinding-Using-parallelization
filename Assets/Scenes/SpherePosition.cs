using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace FinalProject
{
    public class SpherePosition : MonoBehaviour
    {
        private float X;
        private float Y;
        Camera Cm;
        Manager manager;
        //Start is called before the first frame update
        void Start()
        {
            Cm = GameObject.Find("Main Camera").GetComponent<Camera>();
            manager = GameObject.FindObjectOfType<Manager>();
            X = manager.Source.x;
            Y = manager.Source.y;
        }

        IEnumerator MyCoroutine() // creates a coroutine to move the source object towards destination with a delay in each step
        {
            while (manager.path.Count != 0)
            {
                yield return new WaitForSeconds(0.7f);
                X = manager.path.Peek().x;
                Y = manager.path.Peek().y;
                manager.path.Pop();
            }
        }
        // Update is called once per frame
        void Update()
        {
            if (manager.ShowPath)// initiates the coroutine once the script recieves the green light from the manager
            {
                StartCoroutine(MyCoroutine());
                manager.ShowPath = false;
            }
            this.transform.position = Cm.ViewportToScreenPoint(new Vector3((X / 16) + 1 / 32f, (Y / 12) + 1 / 24f, 0));// updates the position of the object 
        }
    }
}
