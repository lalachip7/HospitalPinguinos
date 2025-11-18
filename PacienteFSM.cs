using System;
using UnityEngine;
using UnityEngine.AI;

public class PacienteFSM : MonoBehaviour
{
    // Estructuras de datos (atributos) -------------------------------------------------
    [SerializeField] private float medidorPaciencia = 100f;
    [SerializeField] private bool estaHerido = false;
    [SerializeField] private bool enCamilla = false;
    [SerializeField] private bool sentado = false;
    
    [SerializeField] private EstadoPaciente estadoActual = EstadoPaciente.Inicio;

    private NavMeshAgent navPaciente;       // Componente NavMeshAgent para el movimiento

    [SerializeField] private GameObject puntoColaRecepcion;  // Referencia a la cola
    [SerializeField] private GameObject asientoAsignado;     // Referencia al asiento asignado
    [SerializeField] private GameObject consultaAsignada;    // Referencia a la consulta asignada
    [SerializeField] private GameObject quirofanoAsignado;   // Referencia al quirofano asignado
    [SerializeField] private GameObject salida;              // Referencia a la salida del hospital

    private bool diagnosticoOperacionRecibido = false;   
    private bool diagnosticoAltaRecibido = false;
    private bool operacionRealizada = false;
    private bool celadorLlego = false;


    // Enumeraci?n de estados -----------------------------------------------------------
    public enum EstadoPaciente
    {
        Inicio,
        MoviendoseACola,
        EsperandoEnCola,
        MoviendoseASalaDeEspera,
        EsperandoEnSalaDeEspera,
        MoviendoseAConsulta,
        EsperandoDiagnostico,
        EsperandoTraslado,
        MoviendoseAQuirofano,
        SiendoOperado,
        Saliendo,
        Abandonando,
        Fin
    }

    public void Transicionar(EstadoPaciente nuevoEstado)
    {
        estadoActual = nuevoEstado;
        Debug.Log($"[{gameObject.name}] Transici?n a: {nuevoEstado.ToString()}");
    }

    private void Awake()
    {
        navPaciente = GetComponent<NavMeshAgent>();
        if (navPaciente == null)
        {
            Debug.LogError("El componente NavMeshAgent no est? asignado en el paciente.");
        }
    }

    void Update()
    {
        // Actualiza la paciencia del jugador
        if (estadoActual != EstadoPaciente.Saliendo &&
            estadoActual != EstadoPaciente.Abandonando &&
            estadoActual != EstadoPaciente.Fin)
        {
            medidorPaciencia -= Time.deltaTime * 0.5f; // Decrementa la paciencia con el tiempo

            // Si se le acaba la paciencia, abandona el hospital
            if (PacienciaAgotada()) 
            { 
                MoverAPosicion(salida.transform.position);
                Transicionar(EstadoPaciente.Abandonando);
                return;
            }
        }

        switch (estadoActual)
        {
            case EstadoPaciente.Inicio:

                // Moverse a la cola
                if(puntoColaRecepcion != null)
                {
                    MoverAPosicion(puntoColaRecepcion.transform.position);
                    Transicionar(EstadoPaciente.MoviendoseACola);
                }
                else
                {
                    Debug.LogError("No hay puntoColaRecepcion asignado al paciente");
                }
                break;

            case EstadoPaciente.MoviendoseACola:

                // Comprobar si el paciente ha llegado a la cola
                if (HaLlegadoADestino(puntoColaRecepcion))
                {
                    navPaciente.isStopped = true;
                    Transicionar(EstadoPaciente.EsperandoEnCola);
                    Debug.Log("Paciente en cola y esperando");
                }
                break;

            case EstadoPaciente.EsperandoEnCola:
                break;

            case EstadoPaciente.MoviendoseASalaDeEspera:
                if (HaLlegadoADestino(asientoAsignado))
                {
                    Sentarse();
                    Transicionar(EstadoPaciente.EsperandoEnSalaDeEspera);
                }
                break;

            case EstadoPaciente.EsperandoEnSalaDeEspera:
                break;

            case EstadoPaciente.MoviendoseAConsulta:
                
                if (EstaEnConsulta())
                {
                    OcuparCamilla();
                    Transicionar(EstadoPaciente.EsperandoDiagnostico);
                }
                break;

            case EstadoPaciente.EsperandoDiagnostico:
                if (diagnosticoAltaRecibido)            // Si no necesita operaci?n
                {
                    DejarDinero();
                    MoverAPosicion(salida.transform.position);
                    Transicionar(EstadoPaciente.Saliendo);
                }
                else if (diagnosticoOperacionRecibido)  // Si necesita operaci?n
                {
                    if (EstaHerido())                   // Si est? herido
                    {
                        Transicionar(EstadoPaciente.EsperandoTraslado);
                    } else
                    {
                        Transicionar(EstadoPaciente.MoviendoseAQuirofano);
                        MoverAPosicion(quirofanoAsignado.transform.position);
                    }
                }
                break;

            case EstadoPaciente.EsperandoTraslado:
                if (celadorLlego)
                {
                    Transicionar(EstadoPaciente.MoviendoseAQuirofano);
                    MoverAPosicion(quirofanoAsignado.transform.position);
                }
                break;

            case EstadoPaciente.MoviendoseAQuirofano:
                if (EstaEnQuirofano())
                {
                    OcuparCamilla();
                    Transicionar(EstadoPaciente.SiendoOperado);
                }
                break;

            case EstadoPaciente.SiendoOperado:
                if (operacionRealizada)
                {
                    DejarDinero();
                    MoverAPosicion(salida.transform.position);
                    Transicionar(EstadoPaciente.Saliendo);
                }
                break;

            case EstadoPaciente.Saliendo:
                if (HaLlegadoADestino(salida))
                {
                    AbandonarHospital();
                    Transicionar(EstadoPaciente.Fin);
                }
                break;

            case EstadoPaciente.Abandonando:
                if (HaLlegadoADestino(salida))
                {
                    AbandonarHospital();
                    Transicionar(EstadoPaciente.Fin);
                }
                break;
        }
    }

    public void MoverAPosicion(Vector3 objetivo)
    {
        if (navPaciente != null)
        {
            if (!navPaciente.enabled) navPaciente.enabled = true;
            navPaciente.SetDestination(objetivo);
            navPaciente.isStopped = false;
        }
    }

    public void OcuparCamilla()
    {
        enCamilla = true;
        if (navPaciente != null) navPaciente.enabled = false;
        // Alinear con la camilla
    }

    public void DejarDinero()
    {
        // Instanciar prefab del dinero en la sala
        Debug.Log($"[{gameObject.name}] Deja el dinero en la sala.");
    }

    public void AbandonarHospital()
    {
        Destroy(gameObject);
    }
    private void Sentarse()
    {
        // Animaci?n de sentarse
        sentado = true;
        if (navPaciente != null) navPaciente.enabled = false;
    }

    private void Levantarse()
    {
        // Animaci?n de levantarse
        sentado = false;
        if (navPaciente != null) navPaciente.enabled = true;
    }

    public bool PacienciaAgotada() { return medidorPaciencia <= 0f; }
    public bool ConsultaAsignada() { return consultaAsignada != null; }
    public bool QuirofanoAsignado() { return quirofanoAsignado != null; }
    public bool EstaHerido() { return estaHerido; }
    public bool HaLlegadoADestino(GameObject destino)
    {
        if (navPaciente == null || destino == null) return false;

        float distancia = Vector3.Distance(transform.position, destino.transform.position);

        return distancia <= 0.5f && !navPaciente.pathPending;
    }

    public void RecibirConsultaAsignada(GameObject consulta) 
    {
        if(estadoActual == EstadoPaciente.EsperandoEnCola || estadoActual == EstadoPaciente.EsperandoEnSalaDeEspera)
        {
            consultaAsignada = consulta;
            diagnosticoOperacionRecibido = false;
            diagnosticoAltaRecibido = false;
        }

        if (sentado)
        {
            Levantarse();
        }

        Transicionar(EstadoPaciente.MoviendoseAConsulta);
        MoverAPosicion(consultaAsignada.transform.position);
    }

    public void RecibirAsientoAsignado(GameObject asiento)
    {
        if(estadoActual == EstadoPaciente.EsperandoEnCola)
        {
            asientoAsignado = asiento;
            Transicionar(EstadoPaciente.MoviendoseASalaDeEspera);
            MoverAPosicion(asientoAsignado.transform.position);
        }
    }

    public bool EstaEnConsulta() { return HaLlegadoADestino(consultaAsignada); }
    public bool EstaEnQuirofano() { return HaLlegadoADestino(quirofanoAsignado); }

    public void RecibirDiagnostico( bool requiereOperacion, GameObject quirofano)
    {
        diagnosticoOperacionRecibido = requiereOperacion;
        diagnosticoAltaRecibido = !requiereOperacion;
        quirofanoAsignado = quirofano;
    }
    public void CeladorHaLlegado() { celadorLlego = true; }
    public void OperacionCompletada() { operacionRealizada = true; }

}
