
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

    [SerializeField] private GameObject asientoAsignado;     // Referencia al asiento asignado
    [SerializeField] private GameObject consultaAsignada;    // Referencia a la consulta asignada
    [SerializeField] private GameObject quirofanoAsignado;   // Referencia al quirofano asignado
    [SerializeField] private GameObject salida;              // Referencia a la salida del hospital

    private bool diagnosticoOperacionRecibido = false;   
    private bool diagnosticoAltaRecibido = false;
    private bool operacionRealizada = false;
    private bool celadorLlego = false;


    // Enumeración de estados -----------------------------------------------------------
    public enum EstadoPaciente
    {
        Inicio,
        EsperandoEnCola,
        EsperandoEnSalaDeEspera,
        MoviendoseAConsulta,
        EsperandoDiagnostico,
        EsperandoTraslado,
        SiendoOperado,
        Saliendo,
        Abandonando,
        Fin
    }

    public void Transicionar(EstadoPaciente nuevoEstado)
    {
        estadoActual = nuevoEstado;
        Debug.Log($"[{gameObject.name}] Transición a: {nuevoEstado.ToString()}");
    }

    private void Awake()
    {
        navPaciente = GetComponent<NavMeshAgent>();
        if (navPaciente == null)
        {
            Debug.LogError("El componente NavMeshAgent no está asignado en el paciente.");
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
                // Acceder a la API del recepcionista 
                // Avisar al recepcionista de que ha llegado
                Transicionar(EstadoPaciente.EsperandoEnCola);
                break;

            case EstadoPaciente.EsperandoEnCola:
                if (ConsultaAsignada())
                {
                    MoverAPosicion(consultaAsignada.transform.position);
                    Transicionar(EstadoPaciente.MoviendoseAConsulta);
                }
                else
                {
                    MoverAPosicion(asientoAsignado.transform.position);
                    Transicionar(EstadoPaciente.EsperandoEnSalaDeEspera);
                }
                break;

            case EstadoPaciente.EsperandoEnSalaDeEspera:
                if (ConsultaAsignada() && consultaAsignada != null)
                {
                    MoverAPosicion(consultaAsignada.transform.position);
                    Transicionar(EstadoPaciente.MoviendoseAConsulta);
                }
                break;

            case EstadoPaciente.MoviendoseAConsulta:
                MoverAPosicion(consultaAsignada.transform.position);

                if (EstaEnConsulta())
                {
                    OcuparCamilla();
                    Transicionar(EstadoPaciente.EsperandoDiagnostico);
                }
                break;

            case EstadoPaciente.EsperandoDiagnostico:
                if (diagnosticoAltaRecibido)            // Si no necesita operación
                {
                    DejarDinero();
                    MoverAPosicion(salida.transform.position);
                    Transicionar(EstadoPaciente.Saliendo);
                }
                else if (diagnosticoOperacionRecibido)  // Si necesita operación
                {
                    if (EstaHerido())                   // Si está herido
                    {
                        Transicionar(EstadoPaciente.EsperandoTraslado);
                    } else
                    {
                        MoverAPosicion(quirofanoAsignado.transform.position);
                        Transicionar(EstadoPaciente.SiendoOperado);
                    }
                }
                break;

            case EstadoPaciente.EsperandoTraslado:
                if (celadorLlego)
                {
                    MoverAPosicion(quirofanoAsignado.transform.position);
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
                else
                {
                    MoverAPosicion(salida.transform.position);
                }
                break;

            case EstadoPaciente.Abandonando:
                if (HaLlegadoADestino(salida))
                {
                    AbandonarHospital();
                    Transicionar(EstadoPaciente.Fin);
                }
                else
                {
                    MoverAPosicion(salida.transform.position);
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
        // Animación de sentarse
        sentado = true;
        if (navPaciente != null) navPaciente.enabled = false;
    }

    private void Levantarse()
    {
        // Animación de levantarse
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
        consultaAsignada = consulta; 
        diagnosticoOperacionRecibido = false;
        diagnosticoAltaRecibido = false;
    }
    public void RecibirAsientoAsignado(GameObject asiento) { asientoAsignado = asiento; }

    public bool EstaEnConsulta() { return HaLlegadoADestino(consultaAsignada); }
    public bool EstaEnQuirofano() { return HaLlegadoADestino(quirofanoAsignado); }

    public void RecibirDiagnostico( bool requiereOperacion, GameObject quirofano)
    {
        diagnosticoOperacionRecibido = requiereOperacion;
        diagnosticoOperacionRecibido = !requiereOperacion;
        quirofanoAsignado = quirofano;
    }
    public void CeladorHaLlegado() { celadorLlego = true; }
    public void OperacionCompletada() { operacionRealizada = true; }

}
