import scala.concurrent.duration._
import io.gatling.core.Predef._
import io.gatling.http.Predef._

class MainSimulation extends Simulation {

  val debugConfig = Map("gatling.core.debug.simulation.log.all" -> "true")

  val httpProtocol = http
    .baseUrl("http://localhost:5002/")
    .userAgentHeader("Logging Http Load Test")
    .disableCaching

  val createAndGetAccount = scenario("Create and Get Account")
    .feed(tsv("accounts.tsv").circular())
    .exec(
      http("CreateAndGet")
        .post("/api/account").body(StringBody("#{payload}"))
        .header("content-type", "application/json")
        .header("api-key", "correlation-id-key")
        .check(status.in(201, 422, 400))
        .check(status.saveAs("httpStatus"))
        .checkIf(session => session("httpStatus").as[String] == "201") {
          header("Location").saveAs("location")
        }
    )
    .doIf(session => session.contains("location")) {
      exec(
        http("Get")
          .get("${location}")
          .check(status.in(200,500))
      )
    }

  setUp(
    createAndGetAccount.inject(
      constantUsersPerSec(2).during(10.seconds),
      constantUsersPerSec(5).during(15.seconds),
      rampUsersPerSec(6).to(400).during(3.minutes)
    )
  ).protocols(httpProtocol)
}
