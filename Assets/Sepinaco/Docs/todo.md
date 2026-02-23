quiero crear un script que lo asocie a un gameobject y le incluya al script creado un array de scripts. Los scripts del array que le incluyo son scripts que aparecen en la escena. Quiero que el script creado me muestre un menu debug visual en el que puedo modificar los valores de los atributos que se muestran en el editor de unity de los scripts añadidos respectivamente. Dentro del menu visual del script creado, aparecerá primero una pantalla con el listado de scripts del array en la que puedo navegar uno por uno entre ellos y seleccionarlo. Y una vez seleccionado el script aparecerá otra pantalla con los valores modificables de los atributos del script y puedo navegar entre ellos y modificarlo. Todo lo puedo hacer usando el teclado. Quiero que para abrir y cerrar el menu debug del script que me has creado tenga que pulsar la tecla F1.

======

Crear un script que lo asocie a un gameobject para crear un builder escenario para construir escenarios en runtime y guardarlos y cargarlos y usarlos. Le añado al script los gameobject que puedo crear, eliminar y modificar dentro del juego en un atributo array modificable desde el editor de unity. Y a parte por defecto me muestra todos los gameobjects de la escena para poder añadir nuevos o eliminar los que ya tenemos. Quiero un atributo editable desde el editor de unity para activar o desactivar el modo builder. Cuando activo el modo builder, tengo el control de la cámara en modo dios (tal y como puedo moverme por el editor de unity, con los mismos controles) para poder ver el escenario y viajar por el escenario para modificarlo comodamente.

========

Crear script que añada un gameobject de una esfera
que ocupe todo el mapa sin fisicas para verla como si fuese un cielo desde la escena (para eso la esfera tiene que renderizarse para verlo desde dentro, quiero crear un atributo en el editor de unity para poder modificar si quiero que la esfera se renderice por dentro, por fuera o por ambas partes y que por defecto esté seleccionado por dentro). La esfera tendra de atributos configurables desde el editor de unity la opción de que haga una rotación continua o no la haga junto con los valores modificables de velocidad y dirección de la rotación. También tendrá otro atributo modificable desde el editor de unity para mostrar o no mostrar visualmente la esfera en la escena. También tendrá atributos para modificar la posición, escala y rotación de la esfera. Y tendrá un atributo para poder cambiar la esfera por un cubo o cualquier otro objeto por defecto de unity o un mesh u objeto añadido al array de todos los elementos que hay para reemplazar la esfera. Teniendo definidos algunos por defecto, que son los que vienen por defecto en unity y dando la posibilidad de añadir más o eliminarlos respectivamente desde el menu del editor de unity. Creame el script dentro de la carpeta Sepinaco y crea una nueva carpeta en donde añadas el fichero de ese script.
Hacer que el shader lo tenga que añadir desde el editor para que funcione en la build similar a lo que hacemos en el script ScriptPostProcessingPixelPs1.

========

Mostrarle a la ia el codigo de react de videopoints
(pegar el proyecto en este workspace y no trackearlo con git)
y decirle que me haga una replica del mismo efecto con un script que añada a un gameobject para crear un elemento visual shader mostrando el video que se esté ejecutando en el script ScriptVideoclipsManager (para ello que busque el componente video en la escena para que no dependa del script) y lo muestre con el efecto de los puntos que se realiza en los shaders de webgl. Hacer todos los atributos del shader modificables desde el editor y que funcionen en la build final sin el editor.
Hacer que el shader lo tenga que añadir desde el editor para que funcione en la build similar a lo que hacemos en el script ScriptPostProcessingPixelPs1.

==========


Añadir un script que se asocie a un gameobject para mostrar una vista ui en mobile para simular pulsando botones en mobile a las teclas de PC asociadas a las configuraciones y desplazamientos del menu debug o de cualquier script que se le añada al script creado para poder simular pulsar las teclas en la build de mobile

============

Añadir un script dentro de la carpeta sepinaco, que se asocie a un gameobject para que desactive todos los controles de la camara y del player en la escena y me añada un control en modo dios para poder navegar dentro de la escena como si lo hiciese en el editor de unity y así ir volando por el escenario en todas las direcciones. Quiero un atributo modificable desde el editor de unity para activar o desactivar el control de la camara en modo dios y cuando esté desactivado quiero que tengamos el control que teníamos previamente a activarlo. Estos atributoas también lo podremos modificar cuando añadamos el script creado al script ScriptDebugInspector para modificar esos atributos desde la ventana debug del script ScriptDebugInspector. Quiero que toda la funcionalidad definida funcione cuando haga la build y no solamente en el unity editor.