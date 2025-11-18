using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class RecepcionistaBT : MonoBehaviour
{
    [SerializeField] private Queue<PacienteFSM> colaDeRecepcion = new Queue<PacienteFSM>;           // Pacientes esperando en el mostrador
    [SerializeField] private List<PacienteFSM> listaSalaDeEspera = new List<PacienteFSM>;           // Pacientes en sala de espera
    [SerializeField] private List<PacienteFSM> listaConsultasDisponibles = new List<PacienteFSM>;   // Consultas limpias y lista para usar

    [SerializeField] private Transform puntoMostrador;                  // Posicion idle del recepcionista
    [SerializeField] private Transform zonaNotificacionSalaEspera;      // Punto en la sala de espera para notificatr a paciente

    private PacienteFSM pacienteANotificar;
    private bool estaEnSuPuesto = true;
    private NavMeshAgent agent;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent()>

        if (agent == null)
        {
            Debug.LogError("Recepcionista no tiene un componente NavMeshAgent.");
        }
    }

    void Update()
    {
        // Comprobamos si recepcionista esta en su puesto o en moviento
        if (estaEnSuPuesto)
        {
            GestionarPrioridades();
        }
        else
        {
            ComprobarSiHemosLlegado();
        }
    }

    void GestionarPrioridades()
    {
        if(HaySalaDisponible()&& HayPacientesEsperando())
        {
            PacienteFSM pacientePrioritario = PriorizarPacienteMasImpaciente();

            if (listaSalaDeEspera.Contains(pacientePrioritario))
            {
                MoverASalaYAsignar(pacientePrioritario);
            }
            else
            {
                AsignarConsulta(pacientePrioritario);
            }
        }
        else if (HayPacientesEnMostrador())
        {
            //logica rama 2
        }
        else
        {
            // rama 3 idle - no hat que hacer nada. el recep espera. tecnicamente no hay que programar nada
        }

    }

    bool HaySalaDisponible()
    {
        return listaConsultasDisponibles.Count > 0;     // Comprueba la lista no este vacia
    }

    bool HayPacientesEsperando()
    {
        return colaDeRecepcion.Count > 0 || listaSalaDeEspera.Count > 0;    // Hay alguien en la lista de espera
    }

    bool HayPacienteEnMostrador()
    {
        return colaDeRecepcion.Count > 0; // Comprueba que hayan pacientes en la cola
    }


    // Priorizar paciente mas impaciente / (paciente que lleva mas tiempo esperando)
    PacienteFSM PriorizarPacienteMasImpaciente()
    {
        PacienteFSM pacienteMasImpaciente = null;
        float pacienciaMin = float.MaxValue;

        // Revisamos la cola de recepcion
        foreach (PacienteFSM paciente in colaDeRecepcion)
        {
            if (paciente.medidorPaciencia < pacienciaMin)
            {
                pacienciaMin = paciente.medidorPaciencia;
                pacienteMasImpaciente = paciente;
            }
        }

        // Revisamos la sala de espera
        foreach (PacienteFSM paciente in listaSalaDeEspera)
        {
            if (paciente.medidorPaciencia < pacienciaMin)
            {
                pacienciaMin = paciente.medidorPaciencia;
                pacienteMasImpaciente = paciente;
            }
        }
        return pacienteMasImpaciente
    }
}