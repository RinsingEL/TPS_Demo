using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    public GameObject Arch;
    public GameObject Bullet;
    private static Shoot instance;
    private Vector3 test;

    public static Shoot Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<Shoot>();
            }
            return instance;
        }
    }
    public void ShootBullet(Vector3 target)
    {
        var bl = Instantiate(Bullet, Arch.transform.position, Arch.transform.rotation);
        test = target;
        Vector3 direct = (target - Arch.transform.position).normalized;
        bl.GetComponent<Rigidbody>().AddForce(direct*5000f);
    }
}
