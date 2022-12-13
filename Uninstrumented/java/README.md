# Uninstrumented Java demo app

Requires:

* Java 17+
* A New Relic account

To run the uninstrumented java app via the CLI, switch to the `java` directory and run:

```shell
./gradlew bootRun
```

To exercise, in a new shell:
```shell
./load-generator.sh
```