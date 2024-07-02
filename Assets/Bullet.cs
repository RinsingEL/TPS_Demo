using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 10;

    void OnCollisionEnter(Collision collision)
    {
        // �����ײ�Ķ����Ƿ���"Target"��ǩ
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Health target = collision.gameObject.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
